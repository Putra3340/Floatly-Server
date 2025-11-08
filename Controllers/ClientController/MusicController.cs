using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Xabe.FFmpeg;

namespace Floaty_Music.Controllers.ClientController
{
    public class MusicController : Controller
    {
        public static List<(DateTime cooldownuntil, string token)> cooldowntoken = new();
        public static List<(string Key, string Content, DateTime expiredtime)> StreamSession = new();
        private readonly FloatlyContext _context;

        public MusicController(FloatlyContext cont)
        {
            _context = cont;
        }

        // we use token cooldown, this endpoint just for play count
        // 7 November 2025 we use this to play the music

        [HttpPost("api/play")]
        public async Task<IActionResult> Play(string token, int songId, string bitrate)
        {
            cooldowntoken.RemoveAll(x => x.cooldownuntil <= DateTime.Now); // remove obsolete cooldown
            StreamSession.RemoveAll(x => x.expiredtime <= DateTime.Now); // remove obsolete session

            var allowedBitrates = new[] { "320k", "256k", "192k", "160k", "128k", "96k", "64k" };
            if (!allowedBitrates.Contains(bitrate))
                bitrate = "128k"; // fallback
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Token == token);
            var song = await _context.Songs
                .Include(s => s.Album)
                    .ThenInclude(x => x.Artist)
                .Include(s => s.SongCounter)
                .FirstOrDefaultAsync(s => s.Id == songId);
            if (song == null || song.SongCounter == null)
            {
                return NotFound(new { status = "Error", message = "Song not found." });
            }
            bool onCooldown = cooldowntoken.Any(x => x.token == token && x.cooldownuntil > DateTime.Now);
            if (!onCooldown)
            {
                song.SongCounter.TotalPlayed += 1;
                await _context.SaveChangesAsync();
                if (user != null)
                    cooldowntoken.Add((DateTime.Now.AddMinutes(2), user.Token?? ""));
            }
            var outputPath = GlobalConfiguration.WebRootPath + song.MusicFilePath.Replace(".mp3", $"._compressed_{bitrate}.mp3");
            if (!System.IO.File.Exists(outputPath))
            {
                return BadRequest("Bitrate version not found.");
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}/music/stream/";
            // Return the song file

            string generatedtoken = HashHelper.GenerateRandomLongToken();
            StreamSession.Add((generatedtoken, song.MusicFilePath.Split("music/").Last().Replace(".mp3", $"._compressed_{bitrate}.mp3"), DateTime.Now.AddMinutes(10)));
            return Ok(baseUrl + generatedtoken);
        }


        [HttpGet("music/stream/{filekey}")]
        public IActionResult GetStreamFile(string filekey)
        {
            if (string.IsNullOrEmpty(filekey))
                return NotFound();
            cooldowntoken.RemoveAll(x => x.cooldownuntil <= DateTime.Now); // remove obsolete cooldown
            StreamSession.RemoveAll(x => x.expiredtime <= DateTime.Now); // remove obsolete session
            var session = StreamSession.FirstOrDefault(x => x.Key == filekey);
            if (session == default)
                return NotFound();
            string fileName = session.Content;


            var filePath = Path.Combine(GlobalConfiguration.WebRootPath,GlobalConfiguration.MusicFilePath, fileName);
            if (!filePath.StartsWith(GlobalConfiguration.WebRootPath))
                return BadRequest("Invalid file path.");
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            return PhysicalFile(filePath, "audio/mpeg", enableRangeProcessing: true);
        }


        [HttpGet("api/getqueue")]
        public IActionResult GetRandomNextSong()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var selected = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist).Include(x => x.SongCounter)
    .Take(10)
    .Select(s => new
    {
        Title = s.Title,
        Artist = s.Album.Artist.Name,
        ArtistId = s.Album.Artist.Id,
        ArtistBio = s.Album.Artist.Bio,
        ArtistCover = baseUrl + s.Album.Artist.CoverImagePath,
        Music = baseUrl + s.MusicFilePath,
        Lyrics = baseUrl + s.LyricsFilePath,
        Cover = baseUrl + s.CoverImagePath,
        Banner = baseUrl + s.BannerImagePath,
        SongLength = s.SongCounter.MusicLength,
    })
    .ToList();

            return Ok(selected);
        }
    }
}
