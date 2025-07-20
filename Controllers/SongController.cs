using Floaty_Music.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            Directory.CreateDirectory(GlobalConfiguration.MusicFilePath);
            Directory.CreateDirectory(GlobalConfiguration.LyricsFilePath);
            Directory.CreateDirectory(GlobalConfiguration.CoverImagePath);
            Directory.CreateDirectory(GlobalConfiguration.BannerImagePath);

            string SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
                var fullPath = Path.Combine(folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }

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
                var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
                var fullPath = Path.Combine(folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }
            song.Title = model.Title;
            song.AlbumId = model.AlbumId;

            if (model.MusicFile != null)
                song.MusicFilePath = await SaveFile(model.MusicFile, "music");

            if (model.LyricsFile != null)
                song.LyricsFilePath = await SaveFile(model.LyricsFile, "lyrics");

            if (model.CoverImage != null)
                song.CoverImagePath = await SaveFile(model.CoverImage, "cover");

            if (model.BannerImage != null)
                song.BannerImagePath = await SaveFile(model.BannerImage, "banner");

            await _context.SaveChangesAsync();
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null) return NotFound();

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
