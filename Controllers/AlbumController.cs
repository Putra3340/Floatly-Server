using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AlbumController : Controller
    {
        private readonly FloatlyContext _context;
        public AlbumController(FloatlyContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Create(AlbumFormModel model)
        {
            if (!ModelState.IsValid)
                return Redirect("/Song/Dashboard#albums");
            var album = new Albums
            {
                Title = model.Title,
                ArtistId = model.ArtistId,
                ReleaseDate = model.ReleaseDate,
                CoverImagePath = model.CoverImage != null ? await FileHelper.SaveFileAsync(model.CoverImage, GlobalConfiguration.AlbumCoverPath) : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _context.Albums.AddAsync(album);
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#albums");

        }

        [HttpPost]
        public async Task<IActionResult> Edit(AlbumFormModel model)
        {
            var album = await _context.Albums.FindAsync(model.Id);
            if (album == null)
                return Redirect("/Song/Dashboard#albums");
            album.Title = model.Title;
            album.ArtistId = model.ArtistId;
            if (model.ReleaseDate != null)
                album.ReleaseDate = model.ReleaseDate;
            if (model.CoverImage != null)
                album.CoverImagePath = await FileHelper.SaveFileAsync(model.CoverImage, GlobalConfiguration.AlbumCoverPath);
            album.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#albums");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null)
                return NotFound();

            bool hasSongs = await _context.Songs.AnyAsync(s => s.AlbumId == id);
            if (hasSongs)
                return BadRequest("Cannot delete this album. Delete all songs assigned to it first.");

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#albums");
        }

    }
}
