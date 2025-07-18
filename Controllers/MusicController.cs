using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;

namespace Floaty_Music.Controllers
{
    public class MusicController : Controller
    {
        private readonly FloatlyLibContext _context;
        public MusicController(FloatlyLibContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.MusicList = _context.Library.ToList();
            return View();
        }

        [HttpGet("api/library/{id}")]
        public IActionResult GetLibrary(int id)
        {
            var lib = _context.Library.FirstOrDefault(x => x.Id == id);
            if (lib == null)
                return NotFound(new { message = "Not found" });

            return Json(new
            {
                title = lib.Title,
                artist = lib.ArtistName,
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
            var query = _context.Library.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(x => x.Title.ToUpper().Contains(title.ToUpper()));

            if (!string.IsNullOrWhiteSpace(artist))
                query = query.Where(x => x.ArtistName.ToUpper().Contains(artist.ToUpper()));

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = query.Select(lib => new
            {
                id = lib.Id,
                title = lib.Title,
                artist = lib.ArtistName,
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
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(MusicUploadViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Ensure directories exist
            Directory.CreateDirectory(GlobalConfiguration.MusicFilePath);
            Directory.CreateDirectory(GlobalConfiguration.LyricsFilePath);
            Directory.CreateDirectory(GlobalConfiguration.CoverImagePath);
            Directory.CreateDirectory(GlobalConfiguration.BannerImagePath);

            // Music
            string musicUrl = null;
            if (model.MusicFile != null)
            {
                var fileName = Path.GetRandomFileName() + Path.GetExtension(model.MusicFile.FileName);
                var fullPath = Path.Combine(GlobalConfiguration.MusicFilePath, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.MusicFile.CopyToAsync(stream);
                musicUrl = $"/uploads/music/{fileName}";
            }

            // Lyrics
            string lyricsUrl = null;
            if (model.LyricsFile != null)
            {
                var fileName = Path.GetRandomFileName() + Path.GetExtension(model.LyricsFile.FileName);
                var fullPath = Path.Combine(GlobalConfiguration.LyricsFilePath, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.LyricsFile.CopyToAsync(stream);
                lyricsUrl = $"/uploads/lyrics/{fileName}";
            }

            // Cover
            string coverUrl = null;
            if (model.CoverImage != null)
            {
                var fileName = Path.GetRandomFileName() + Path.GetExtension(model.CoverImage.FileName);
                var fullPath = Path.Combine(GlobalConfiguration.CoverImagePath, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.CoverImage.CopyToAsync(stream);
                coverUrl = $"/uploads/cover/{fileName}";
            }

            // Banner
            string bannerUrl = null;
            if (model.BannerImage != null)
            {
                var fileName = Path.GetRandomFileName() + Path.GetExtension(model.BannerImage.FileName);
                var fullPath = Path.Combine(GlobalConfiguration.BannerImagePath, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.BannerImage.CopyToAsync(stream);
                bannerUrl = $"/uploads/banner/{fileName}";
            }

            // Save to DB
            var library = new Library
            {
                Title = model.Title,
                ArtistName = model.ArtistName,
                MusicFilePath = musicUrl,
                LyricsFilePath = lyricsUrl,
                CoverImagePath = coverUrl,
                BannerImagePath = bannerUrl,
                CreatedAt = DateTime.Now
            };

            _context.Library.Add(library);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Upload successful!";
            return View();
        }
    }
}
