using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Controllers
{
    [Route("api/library/v1")]
    [ApiController]
    public class LibraryV1Controller : ControllerBase
    {
        private readonly FloatlyContext _context;
        public LibraryV1Controller(FloatlyContext cont)
        {
            _context = cont;
        }

        [HttpGet("{id}")]
        public IActionResult GetLibrary(int id)
        {
            var lib = _context.Songs.Include(a => a.Album).ThenInclude(a => a.Artist)
                                    .FirstOrDefault(x => x.Id == id);
            if (lib == null)
                return NotFound(new { message = "Not found" });

            return Ok(new
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

        [HttpGet]
        public IActionResult GetLibraries([FromQuery] string? title, [FromQuery] string? artist)
        {
            var query = _context.Songs.Include(a => a.Album).ThenInclude(a => a.Artist).AsQueryable();

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

            return Ok(result);
        }
    }
}
