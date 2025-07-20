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

        // GET: /Artist
        public IActionResult Index()
        {
            var artists = _context.Artists.OrderDescending().ToList();
            return View(artists);
        }

        // GET: /Artist/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Artist/Create
        [HttpPost]
        public IActionResult Create(Artists artist)
        {
            if (ModelState.IsValid)
            {
                _context.Artists.Add(artist);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(artist);
        }

        // GET: /Artist/Edit/5
        public IActionResult Edit(int id)
        {
            var artist = _context.Artists.Find(id);
            if (artist == null) return NotFound();
            return View(artist);
        }

        // POST: /Artist/Edit/5
        [HttpPost]
        public IActionResult Edit(Artists artist)
        {
            if (ModelState.IsValid)
            {
                _context.Artists.Update(artist);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(artist);
        }

        // GET: /Artist/Delete/5
        public IActionResult Delete(int id)
        {
            var artist = _context.Artists.Find(id);
            if (artist == null) return NotFound();
            return View(artist);
        }

        // POST: /Artist/Delete/5
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var artist = _context.Artists.Find(id);
            if (artist != null)
            {
                _context.Artists.Remove(artist);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
