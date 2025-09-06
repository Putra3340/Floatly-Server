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
        public IActionResult Create(Albums album)
        {
            _context.Albums.Add(album);
            _context.SaveChanges();
            return Redirect("/Song/Dashboard#albums");

        }

        [HttpPost]
        public IActionResult Edit(Albums album)
        {
            var albumbak = _context.Albums.Find(album.Id);
            if (albumbak == null)
                return NotFound();
            //_context.Albums.Update(album);
            albumbak.Title = album.Title ?? albumbak.Title;
            albumbak.Artist = album.Artist ?? albumbak.Artist;
            albumbak.ReleaseDate = album.ReleaseDate ?? albumbak.ReleaseDate;
            albumbak.CoverUrl = album.CoverUrl ?? albumbak.CoverUrl;
            _context.SaveChanges();
            return Redirect("/Song/Dashboard#albums");
        }

        [HttpPost]
        public IActionResult Delete(long id)
        {
            var album = _context.Albums.Find(id);
            if (album != null)
            {
                _context.Albums.Remove(album);
                _context.SaveChanges();
            }
            return Redirect("/Song/Dashboard#albums");
        }
        public IActionResult Index()
        {
            var albums = _context.Albums.OrderDescending().Include(a => a.Artist).ToList();
            ViewBag.Artists = _context.Artists.ToList(); // for dropdown
            return View(albums);
        }

    }
}
