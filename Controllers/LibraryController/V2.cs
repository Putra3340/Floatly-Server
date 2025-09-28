using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Controllers
{
    [Route("api/library/v2")]
    [ApiController]
    public class LibraryV2Controller : ControllerBase
    {
        private readonly FloatlyContext _context;
        public LibraryV2Controller(FloatlyContext cont)
        {
            _context = cont;
        }
        [HttpGet]
        public IActionResult Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var songlist = _context.Songs
                .Include(s => s.Album).ThenInclude(a => a.Artist)
                .Include(s => s.SongCounter)
                .OrderByDescending(s => s.SongCounter.TotalPlayed) // sort by plays
                .Take(5)
                .ToList();

            var topSongs = songlist.Select(x => new
            {
                id = x.Id,
                title = x.Title,
                artist = x.Album.Artist.Name,
                music = baseUrl + x.MusicFilePath,
                lyrics = baseUrl + x.LyricsFilePath,
                cover = baseUrl + x.CoverImagePath,
                banner = baseUrl + x.BannerImagePath,
                songlength = x.SongCounter.MusicLength,
                playcount = x.SongCounter.TotalPlayed,
                createdAt = x.CreatedAt
            }).ToList();

            var topArtists = _context.Artists
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    coverUrl = baseUrl + a.CoverImagePath,
                    totalPlays = a.Albums
                        .SelectMany(al => al.Songs)
                        .Sum(s => (long?)s.SongCounter.TotalPlayed ?? 0)
                })
                .OrderByDescending(a => a.totalPlays)
                .Take(3)
                .ToList();

            var topAlbums = _context.Albums
                .Select(al => new
                {
                    id = al.Id,
                    title = al.Title,
                    artistName = al.Artist.Name,
                    coverUrl = baseUrl + al.CoverImagePath,
                    totalPlays = al.Songs.Sum(s => (long?)s.SongCounter.TotalPlayed ?? 0)
                })
                .OrderByDescending(al => al.totalPlays)
                .Take(3)
                .ToList();


            var result = new
            {
                songs = topSongs,
                artists = topArtists,
                albums = topAlbums
            };
            return Ok(result);
        }

        [HttpGet("search")] // search
        public IActionResult Search([FromQuery] string? anycontent)
        {
            if (string.IsNullOrWhiteSpace(anycontent))
                return Ok(new { songs = new List<object>(), artists = new List<object>(), albums = new List<object>() });

            var songlist = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist)
                .Where(x => x.Title.ToUpper().Contains(anycontent.ToUpper())).ToList();
            var artistlist = _context.Artists.Where(x => x.Name.ToUpper().Contains(anycontent.ToUpper())).ToList();
            var albumlist = _context.Albums.Include(x => x.Artist).Where(x => x.Title.ToUpper().Contains(anycontent.ToUpper())).ToList();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = new
            {
                songs = songlist.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    artist = x.Album.Artist.Name,
                    downloadUrls = new
                    {
                        music = baseUrl + x.MusicFilePath,
                        lyrics = baseUrl + x.LyricsFilePath,
                        cover = baseUrl + x.CoverImagePath,
                        banner = baseUrl + x.BannerImagePath
                    },
                    createdAt = x.CreatedAt
                }).Take(5).ToList(), // limit
                artists = artistlist.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    profileUrl = baseUrl + x.CoverImagePath,
                }).Take(3).ToList(),
                albums = albumlist.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    artistName = x.Artist.Name,
                    coverUrl = baseUrl + x.CoverImagePath
                }).Take(3).ToList()
            };
            return Ok(result);
        }

        [HttpGet("{id}")]
        public IActionResult GetSong(int id)
        {
            var lib = _context.Songs.Include(a => a.Album).ThenInclude(a => a.Artist)
                                    .FirstOrDefault(x => x.Id == id);
            if (lib == null)
                return NotFound(new { message = "Not found" });

            return Ok(new
            {
                id = lib.Id,
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
        [HttpGet("artist/{id}")]
        public IActionResult SearchArtist(int id)
        {
            var artis = _context.Artists.Find(id);
            if (artis == null) return NotFound(new { message = "Not found" });
            return Ok(artis);
        }

        [HttpGet("album/{id}")]
        public IActionResult SearchAlbum(int id)
        {
            var album = _context.Albums.Find(id);
            if (album == null) return NotFound(new { message = "Not found" });
            return Ok(album);
        }
    }
}
