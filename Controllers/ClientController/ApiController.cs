using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Diagnostics;

namespace Floaty_Music.Controllers.ClientController
{
    public class ApiController : Controller
    {
        private readonly FloatlyContext _context;
        public ApiController(FloatlyContext cont)
        {
            _context = cont;
        }

        [HttpGet("api/info")]
        public async Task<IActionResult> Check()
        {
            var response = new
            {
                status = GlobalConfiguration.ServerStatus,
                message = GlobalConfiguration.ServerDetail,
                version = "EarlyAccess-1.0.0",
                uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                serverName = Environment.MachineName,
                serverdetail = "EarlyAccess Server",
                totalsong = _context.Songs.Count() + _context.YoutubeSongs.Count(),
                totalartist = _context.Artists.Count(),
                totalalbums = _context.Albums.Count()
            };
            return Json(response);
        }

        // 4 January 2026: IDC deadline is near
        [HttpGet("api/ads")]
        public async Task<IActionResult> GetAds()
        {
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.Name == "Floaty");
            if (artist == null)
            {
                return BadRequest();
            }
            var album = await _context.Albums.FirstOrDefaultAsync(a => a.Title == "Advertisements" && a.ArtistId == artist.Id);
            if (album == null)
            {
                return BadRequest();
            }
            var songs = await _context.Songs.Include(x => x.SongCounter).Include(x => x.Album).ThenInclude(x => x.Artist).Where(x => x.Album.Title == album.Title && x.Album.Artist.Name == artist.Name).ToListAsync();
            if(songs.Count == 0)
            {
                return BadRequest();
            }
            var songdb = songs[new Random().Next(songs.Count)];
            var baseUrl = $"{Request.Scheme}://{Request.Host}/uploads/";
            var song = new ApiSongPlay()
            {
                AlbumId = songdb.Album.Id,
                AlbumTitle = songdb.Album.Title,
                ArtistId = songdb.Album.Artist.Id.ToString(),
                ArtistName = songdb.Album.Artist.Name,
                Cover = $"{baseUrl}/cover/{songdb.CoverImagePath}",
                Banner = $"{baseUrl}/banner/{songdb.BannerImagePath}",
                CreatedAt = songdb.CreatedAt,
                Id = songdb.Id.ToString(),
                Title = songdb.Title,
                Lyrics = songdb.LyricsFilePath != null ? $"{baseUrl}/lyrics/{songdb.LyricsFilePath}" : null,
                Music = songdb.MusicFilePath != null ? $"{baseUrl}/music/{songdb.MusicFilePath}" : null,
                MoviePath = songdb.MoviePath != null ? $"{baseUrl}/video/{songdb.MoviePath}" : null,
                UploadedBy = songdb.UploadedBy,
                SongLength = TimeSpan.FromSeconds((double)songdb.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                PlayCount = (songdb.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
            };
            // increment play count
            if (songdb?.SongCounter != null)
            {
                var counter = songdb.SongCounter.FirstOrDefault();
                if (counter != null)
                {
                    counter.TotalPlayed += 1;
                    await _context.SaveChangesAsync();
                }
            }
            return Json(song);
        }
    }
}
