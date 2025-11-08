using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArtistController : Controller
    {
        private readonly FloatlyContext _context;
        public ArtistController(FloatlyContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Create(ArtistFormModel model)
        {
            if (!model.Name.isNotNullOrWhiteSpace())
                return BadRequest();
            var artist = new Artists
            {
                Name = model.Name,
                Bio = model.Bio,
                CoverImagePath = model.ProfileUrl != null ? await FileHelper.SaveFileAsync(model.ProfileUrl, GlobalConfiguration.ArtistProfilePath) : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _context.Artists.AddAsync(artist);
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#artists");
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ArtistFormModel model)
        {
            var artist = await _context.Artists.FindAsync(model.Id);
            if (artist == null)
                return Redirect("/Song/Dashboard#artists");
            artist.Name = model.Name;
            artist.Bio = model.Bio;
            if (model.ProfileUrl != null)
                artist.CoverImagePath = await FileHelper.SaveFileAsync(model.ProfileUrl, GlobalConfiguration.ArtistProfilePath);
            artist.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#artists");
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var artist = await _context.Artists.FindAsync(id);
            if (artist == null)
                return NotFound("Artist not found.");

            bool hasAlbums = await _context.Albums.AnyAsync(a => a.ArtistId == id);
            if (hasAlbums)
                return BadRequest("Cannot delete this artist. Delete all albums assigned to this artist first.");

            _context.Artists.Remove(artist);
            await _context.SaveChangesAsync();

            return Redirect("/Song/Dashboard#artists");
        }

    }
}
