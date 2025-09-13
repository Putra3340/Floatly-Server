using Floaty_Music.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            if (!ModelState.IsValid)
                return Redirect("/Song/Dashboard#artists");
            async Task<string> SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // 4.59 x 10^-43% Chance for collisions
                var fullPath = Path.Combine(GlobalConfiguration.WebRootPath, GlobalConfiguration.UploadsFolder, folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }
            var artist = new Artists
            {
                Name = model.Name,
                Bio = model.Bio,
                ProfileUrl = model.ProfileUrl != null ? await SaveFile(model.ProfileUrl, GlobalConfiguration.ArtistProfilePath) : null,
                // CreatedAt = DateTime.Now,
                // UpdatedAt = DateTime.Now
            };
            _context.Artists.Add(artist);
            _context.SaveChanges();
            return Redirect("/Song/Dashboard#artists");
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ArtistFormModel model)
        {
            var artist = await _context.Artists.FindAsync(model.Id);
            if (artist == null)
                return Redirect("/Song/Dashboard#artists");
            async Task<string> SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // 4.59 x 10^-43% Chance for collisions
                var fullPath = Path.Combine(GlobalConfiguration.WebRootPath, GlobalConfiguration.UploadsFolder, folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }
            artist.Name = model.Name;
            artist.Bio = model.Bio;
            if (model.ProfileUrl != null)
                artist.ProfileUrl = await SaveFile(model.ProfileUrl, GlobalConfiguration.ArtistProfilePath);
            // artist.UpdatedAt = DateTime.Now;
            _context.Artists.Update(artist);
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#artists");
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var artist = _context.Artists.Find(id);
            if (artist != null)
            {
                _context.Artists.Remove(artist);
                _context.SaveChanges();
            }
            return Redirect("/Song/Dashboard#artists");
        }
    }
}
