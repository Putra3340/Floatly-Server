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
                .OrderByDescending(s => s.SongCounter.FirstOrDefault().TotalPlayed) // sort by plays
                .Take(10)
                .ToList();

            var topSongs = songlist.Select(x => new
            {
                id = x.Id,
                title = x.Title,
                artistId = x.Album?.Artist?.Id,
                artistName = x.Album?.Artist?.Name,
                music = baseUrl + x.MusicFilePath,
                lyrics = baseUrl + x.LyricsFilePath,
                cover = baseUrl + x.CoverImagePath,
                banner = baseUrl + x.BannerImagePath,
                songLength = x.SongCounter.FirstOrDefault()?.MusicLength ?? 0,
                playCount = x.SongCounter.FirstOrDefault()?.TotalPlayed,
                createdAt = x.CreatedAt
            }).AsEnumerable() // switch to LINQ-to-Objects
    .Select(x => new
    {
        x.id,
        x.title,
        x.artistId,
        x.artistName,
        x.music,
        x.lyrics,
        x.cover,
        x.banner,
        songLength = TimeSpan.FromSeconds(x.songLength).ToString(@"mm\:ss"),
        playCount = (x.playCount ?? 0).ToString("N0") + " Plays",
        x.createdAt
    })
    .ToList();

            var topArtists = _context.Artists
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    coverUrl = baseUrl + a.CoverImagePath,
                    totalPlays = a.Albums
                        .SelectMany(al => al.Songs)
                        .Sum(s => (long?)s.SongCounter.FirstOrDefault().TotalPlayed ?? 0)
                })
                .OrderByDescending(a => a.totalPlays)
                .Take(6)
                .AsEnumerable()
                .Select(a => new
                {
                    a.id,
                    a.name,
                    a.coverUrl,
                    totalPlays = a.totalPlays.ToString("N0") + " Plays"
                })
                .ToList();

            var topAlbums = _context.Albums
                .Select(al => new
                {
                    id = al.Id,
                    title = al.Title,
                    artistName = al.Artist.Name,
                    coverUrl = baseUrl + al.CoverImagePath,
                    totalPlays = al.Songs.Sum(s => (long?)s.SongCounter.FirstOrDefault().TotalPlayed ?? 0)
                })
                .OrderByDescending(al => al.totalPlays)
                .Take(10)
                .AsEnumerable()
                .Select(al => new
                {
                    id = al.id,
                    title = al.title,
                    artistName = al.artistName,
                    coverUrl = al.coverUrl,
                    totalPlays = (al.totalPlays).ToString("N0") + " Plays"
                })
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
            List<Songs>? songlist = null;
            List<Artists> artistlist = null;
            List<Albums> albumlist = null;
            if (string.IsNullOrWhiteSpace(anycontent)) // fetch some if empty
            {
                songlist = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist).Include(x=>x.SongCounter).Take(20).ToList();
                artistlist = _context.Artists.Take(5).ToList();
                albumlist = _context.Albums.Include(x => x.Artist).Take(20).ToList();
            }
            else // filter search
            {
                songlist = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist).Include(x=>x.SongCounter).Where(x => x.Title.ToUpper().Contains(anycontent.ToUpper())).ToList();
                artistlist = _context.Artists.Where(x => x.Name.ToUpper().Contains(anycontent.ToUpper())).ToList();
                albumlist = _context.Albums.Include(x => x.Artist).Where(x => x.Title.ToUpper().Contains(anycontent.ToUpper())).ToList();
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = new
            {
                songs = songlist.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    artistId = x.Album?.Artist?.Id,
                    artistName = x.Album?.Artist?.Name,
                    music = baseUrl + x.MusicFilePath,
                    lyrics = baseUrl + x.LyricsFilePath,
                    cover = baseUrl + x.CoverImagePath,
                    banner = baseUrl + x.BannerImagePath,
                    songLength = x.SongCounter.FirstOrDefault()?.MusicLength ?? 0,
                    playCount = x.SongCounter.FirstOrDefault()?.TotalPlayed,
                    createdAt = x.CreatedAt
                }).AsEnumerable() .Select(x => new{x.id,x.title,x.artistName,x.artistId,x.music,x.lyrics,x.cover,x.banner,songLength = TimeSpan.FromSeconds(x.songLength).ToString(@"mm\:ss"),playCount = (x.playCount ?? 0).ToString("N0") + " Plays",x.createdAt})
    .ToList(),
            artists = artistlist.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    coverUrl = baseUrl + x.CoverImagePath,
                }).ToList(),
                albums = albumlist.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    artistName = x.Artist.Name,
                    coverUrl = baseUrl + x.CoverImagePath
                }).ToList()
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
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var artis = _context.Artists.Find(id);
            if (artis == null) return NotFound(new { message = "Not found" });
            artis.CoverImagePath = baseUrl + artis.CoverImagePath;

            return Ok(new
            {
                Id = artis.Id,
                Bio = artis.Bio,
                Name = artis.Name,
                CoverUrl = artis.CoverImagePath,
                CreatedAt = artis.CreatedAt,
                UpdatedAt = artis.UpdatedAt
            });
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
