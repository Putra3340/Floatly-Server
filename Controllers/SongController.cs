using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TagLib;

namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SongController : Controller
    {
        private readonly FloatlyContext _context;
        public SongController(FloatlyContext context) => _context = context;

        public IActionResult Index()
        {
            ViewBag.Artists = _context.Artists.ToList();
            ViewBag.Albums = _context.Albums.ToList();
            var songs = _context.Songs.Include(s => s.Album).ToList();
            return View(songs);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(SongUploadModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            string SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // 4.59 x 10^-43% Chance for collisions
                var fullPath = Path.Combine(folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }
            double musiclength = 0;
            var song = new Songs
            {
                Title = model.Title,
                AlbumId = model.AlbumId,
                MusicFilePath = model.MusicFile != null ? SaveFile(model.MusicFile, GlobalConfiguration.MusicFilePath) : null,
                LyricsFilePath = model.LyricsFile != null ? SaveFile(model.LyricsFile, GlobalConfiguration.LyricsFilePath) : null,
                CoverImagePath = model.CoverImage != null ? SaveFile(model.CoverImage, GlobalConfiguration.CoverImagePath) : null,
                BannerImagePath = model.BannerImage != null ? SaveFile(model.BannerImage, GlobalConfiguration.BannerImagePath) : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Extract duration from MP3 file
            if (!Path.GetExtension(model.MusicFile.FileName).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only MP3 allowed");
            using var stream = model.MusicFile.OpenReadStream();
            var tagFile = TagLib.File.Create(new StreamFileAbstraction(model.MusicFile.FileName, stream));
            musiclength = tagFile.Properties.Duration.TotalSeconds;

            song.SongCounter = new SongCounter
            {
                TotalLikes = 0,
                TotalPlayed = 0,
                MusicLength = (int)musiclength
            };
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SongUploadModel model)
        {
            var song = await _context.Songs.FindAsync(model.Id);
            if (song == null) return NotFound();
            async Task<string> SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // 4.59 x 10^-43% Chance for collisions
                var fullPath = Path.Combine(GlobalConfiguration.WebRootPath,GlobalConfiguration.UploadsFolder,folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }
            song.Title = model.Title;
            song.AlbumId = model.AlbumId;

            double musiclength = 0;

            if (model.MusicFile != null)
            {
                // Extract duration from MP3 file
                if (!Path.GetExtension(model.MusicFile.FileName).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Only MP3 allowed");
                using var stream = model.MusicFile.OpenReadStream();
                var tagFile = TagLib.File.Create(new StreamFileAbstraction(model.MusicFile.FileName, stream));
                musiclength = tagFile.Properties.Duration.TotalSeconds;
                // Save
                song.MusicFilePath = await SaveFile(model.MusicFile, "music");
            }
            if (model.LyricsFile != null)
                song.LyricsFilePath = await SaveFile(model.LyricsFile, "lyrics");

            if (model.CoverImage != null)
                song.CoverImagePath = await SaveFile(model.CoverImage, "cover");

            if (model.BannerImage != null)
                song.BannerImagePath = await SaveFile(model.BannerImage, "banner");

            await _context.SaveChangesAsync();

            var songcounter = await _context.SongCounter.FirstOrDefaultAsync(x => x.SongId == song.Id);
            if (songcounter != null)
            {
                songcounter.MusicLength = (int)musiclength==0?songcounter.MusicLength:(int)musiclength;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            var songcount = await _context.SongCounter.FindAsync(id);
            if (song == null) return NotFound();

            _context.SongCounter.Remove(songcount);
            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        
        public IActionResult Dashboard()
        {
            var totalSongs = _context.Songs.Count();
            var totalAlbums = _context.Albums.Count();
            var totalArtists = _context.Artists.Count();
            ViewBag.TotalSongs = totalSongs;
            ViewBag.TotalAlbums = totalAlbums;
            ViewBag.TotalArtists = totalArtists;
            ViewBag.TotalPlayed = _context.SongCounter.Sum(x => x.TotalPlayed);
            ViewBag.TotalLikes = _context.SongCounter.Sum(x => x.TotalLikes);
            ViewBag.TopSongs = _context.SongCounter
                .OrderByDescending(x => x.TotalPlayed)
                .Take(5)
                .Include(x => x.Song)
                    .ThenInclude(s => s.Album)
                        .ThenInclude(a => a.Artist)
                .ToList();
            ViewBag.Albums = _context.Albums.OrderDescending().Include(a => a.Artist).ToList();
            ViewBag.Artists = _context.Artists.OrderDescending().ToList(); // for dropdown
            ViewBag.Songs = _context.Songs
    .OrderByDescending(x => x.Id)
    .Include(x => x.Album)
        .ThenInclude(a => a.Artist)
    .Include(x => x.SongCounter)
    .ToList();

            ViewBag.RecentlyAddedSongs = _context.Songs
    .OrderByDescending(x => x.CreatedAt).ToList();
            return View();
        }
    }
}
