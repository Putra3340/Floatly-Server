using Floaty_Music.Models;
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

            async Task<string> SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // 4.59 x 10^-43% Chance for collisions
                var fullPath = Path.Combine(GlobalConfiguration.WebRootPath, GlobalConfiguration.UploadsFolder, folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }

            var album = new Albums
            {
                Title = model.Title,
                ArtistId = model.ArtistId,
                ReleaseDate = model.ReleaseDate,
                CoverUrl = model.CoverImage != null ? SaveFile(model.CoverImage, GlobalConfiguration.AlbumCoverPath).Result : null,
                // CreatedAt = DateTime.Now,
                // UpdatedAt = DateTime.Now
            };
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#albums");
        
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AlbumFormModel model)
        {
            var album = await _context.Albums.FindAsync(model.Id);
            if (album == null)
                return Redirect("/Song/Dashboard#albums");
            async Task<string> SaveFile(IFormFile file, string folder)
            {
                var fileName = Path.GetRandomFileName() + Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // 4.59 x 10^-43% Chance for collisions
                var fullPath = Path.Combine(GlobalConfiguration.WebRootPath, GlobalConfiguration.UploadsFolder, folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
            }
            album.Title = model.Title;
            album.ArtistId = model.ArtistId;
            if(model.ReleaseDate != null)
                album.ReleaseDate = model.ReleaseDate;
            if (model.CoverImage != null)
                album.CoverUrl = await SaveFile(model.CoverImage, GlobalConfiguration.AlbumCoverPath);
            // album.UpdatedAt = DateTime.Now;
            _context.Albums.Update(album);
            await _context.SaveChangesAsync();
            return Redirect("/Song/Dashboard#albums");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var album = _context.Albums.Find(id);
            if (album != null)
            {
                _context.Albums.Remove(album);
                _context.SaveChanges();
            }
            return Redirect("/Song/Dashboard#albums");
        }
    }
}
