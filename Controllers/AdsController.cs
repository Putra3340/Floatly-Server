using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Floaty_Music.Controllers.SongController;

namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdsController : Controller
    {
        private readonly FloatlyContext _context;
        public AdsController(FloatlyContext db)
        {
            _context = db;
        }
        public async Task Init()
        {
            var artist = new Artists
            {
                Name = "Floaty",
                Bio = "Official advertisement channel for Floaty Music.",
                CoverImagePath = "/images/ads/floaty_ads_artist.jpg",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var album = new Albums
            {
                Title = "Advertisements",
                CoverImagePath = "/images/ads/floaty_ads_cover.jpg",
                ReleaseDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var arexist = _context.Artists.FirstOrDefault(x => x.Name == artist.Name);
            var alexist = _context.Albums.FirstOrDefault(x => x.Title == album.Title);
            if(arexist != null || alexist != null)
            {
                return;
            }


            _context.Artists.Add(artist);
            await _context.SaveChangesAsync();
            album.ArtistId = artist.Id;
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            return;
        }
        [HttpGet]
        public async Task<IActionResult> GetAdsSong()
        {
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.Name == "Floaty");
            if (artist == null)
            {
                await Init();
                return BadRequest();
            }
            var album = await _context.Albums.FirstOrDefaultAsync(a => a.Title == "Advertisements" && a.ArtistId == artist.Id);
            if (album == null)
            {
                await Init();
                return BadRequest();
            }
            var songs = await _context.Songs.Include(x => x.SongCounter).OrderDescending().Select(s =>
            new
            {
                s.Id,
                s.Title,
                s.Hidden,
                s.Highlighted,
                MusicFilePath = "/uploads/music/" + s.MusicFilePath
            }).ToListAsync();
            return Json(songs);
        }
        [HttpPost]
        public async Task<IActionResult> Upload(string title, IFormFile file)
        {
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.Name == "Floaty");
            if (artist == null)
            {
                await Init();
                return BadRequest();
            }
            var album = await _context.Albums.FirstOrDefaultAsync(a => a.Title == "Advertisements" && a.ArtistId == artist.Id);
            if (album == null)
            {
                await Init();
                return BadRequest();
            }
            double musiclength = 0;
            var song = new Songs
            {
                Title = title,
                AlbumId = album.Id,
                MusicFilePath = file != null ? await FileHelper.SaveIFormFileAsync(file, FileHelper.UploadFolder.Music) : null,
                LyricsFilePath = "ads.png",
                CoverImagePath = "ads.png",
                BannerImagePath = "ads.png",
                MoviePath = "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            // Extract duration from MP3 file
            //musiclength = await Task.Run(() =>
            //{
            //    using var stream = model.MusicFile.OpenReadStream();
            //    var tagFile = TagLib.File.Create(new StreamFileAbstraction(model.MusicFile.FileName, stream));
            //    return tagFile.Properties.Duration.TotalSeconds;
            //});
            song.SongCounter = new SongCounter[]
            {
                new SongCounter{
                    TotalLikes = 0,
                TotalPlayed = 0,
                MusicLength = (int)musiclength
                }
            };
            await _context.Songs.AddAsync(song);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> HideSong([FromBody] HideSongRequest req)
        {
            var song = await _context.Songs.Where(x => x.Id == req.Id).FirstOrDefaultAsync();
            song.Hidden = req.Hidden;
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> HighSong([FromBody] HideSongRequest req)
        {
            var song = await _context.Songs.Where(x => x.Id == req.Id).FirstOrDefaultAsync();
            song.Highlighted = req.Hidden;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
