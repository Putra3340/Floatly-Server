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
            ViewBag.MusicList = _context.Songs.ToList();
            return View();
        }
        [HttpGet("api/info")]
        public IActionResult Check()
        {
            var response = new
            {
                status = "Active",
                message = "Floaty Music is running smoothly.",
                version = "1.0.0",
                uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                serverName = Environment.MachineName,
                serverdetail = "Development Server",
                totalsong = _context.Songs.Count(),
            };
            return Json(response);
        }

        [HttpGet("api/library/{id}")]
        public IActionResult GetLibrary(int id)
        {
            var lib = _context.Songs.Include(a => a.Album).ThenInclude(a=>a.Artist).FirstOrDefault(x => x.Id == id);
            if (lib == null)
                return NotFound(new { message = "Not found" });

            return Json(new
            {
                title = lib.Title,
                artist = lib.Album.Artist.Name,
                downloadUrls = new
                {
                    music = lib.MusicFilePath,
                    lyrics = lib.LyricsFilePath,
                    cover = lib.CoverImagePath,
                    banner = lib.BannerImagePath
                },
                createdAt = lib.CreatedAt
            });
        }
        [HttpGet("api/library")]
        public IActionResult GetLibraries([FromQuery] string? title, [FromQuery] string? artist)
        {
            var query = _context.Songs.Include(a=>a.Album).ThenInclude(a=>a.Artist).AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(x => x.Title.ToUpper().Contains(title.ToUpper()));

            if (!string.IsNullOrWhiteSpace(artist))
                query = query.Where(x => x.Album.Artist.Name.ToUpper().Contains(artist.ToUpper()));

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = query.Select(lib => new
            {
                id = lib.Id,
                title = lib.Title,
                artist = lib.Album.Artist.Name,
                downloadUrls = new
                {
                    music = baseUrl + lib.MusicFilePath,
                    lyrics = baseUrl + lib.LyricsFilePath,
                    cover = baseUrl + lib.CoverImagePath,
                    banner = baseUrl + lib.BannerImagePath
                },
                createdAt = lib.CreatedAt
            }).ToList();

            return Json(result);
        }
        
    }
}
