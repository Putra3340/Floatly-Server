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
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Edit(Albums album)
        {
            _context.Albums.Update(album);
            _context.SaveChanges();
            return RedirectToAction("Index");
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
            return RedirectToAction("Index");
        }
        public IActionResult Index()
        {
            var albums = _context.Albums.OrderDescending().Include(a => a.Artist).ToList();
            ViewBag.Artists = _context.Artists.ToList(); // for dropdown
            return View(albums);
        }

    }
}
