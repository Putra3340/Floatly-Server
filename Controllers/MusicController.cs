using Floaty_Music.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MusicController : Controller
    {
        private readonly FloatlyContext _context;
        public MusicController(FloatlyContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.MusicList = _context.Songs.Include(a=>a.Album).ThenInclude(b=>b.Artist).ToList();
            return View();
        }
        
        
    }
}
