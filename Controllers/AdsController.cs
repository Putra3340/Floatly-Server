using Floaty_Music.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var album = new Albums
            {
                Title = "Advertisements",
                CoverImagePath = "/images/ads/floaty_ads_cover.jpg",
                ReleaseDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


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
                s.Highlighted
            }).ToListAsync();
            return Json(songs);
        }
    }
}
