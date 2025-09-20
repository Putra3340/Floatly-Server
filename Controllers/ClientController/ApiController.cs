using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Floaty_Music.Controllers.ClientController
{
    public class ApiController : Controller
    {
        public static List<(DateTime cooldownuntil,string token)> cooldowntoken = new();
        private readonly FloatlyContext _context;
        public ApiController(FloatlyContext cont)
        {
            _context = cont;
        }

        [HttpGet("api/info")]
        public IActionResult Check()
        {
            var response = new
            {
                status = "Active",
                message = "Floaty Music Server is in progress.",
                version = "1.0.1",
                uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                serverName = Environment.MachineName,
                serverdetail = "Development Server",
                totalsong = _context.Songs.Count(),
                totalartist = _context.Artists.Count(),
                totalalbums = _context.Albums.Count()
            };
            return Json(response);
        }

        [HttpPost("api/play")] // we use token cooldown, this endpoint just for play count
        public IActionResult Play(string token, int songId)
        {
            cooldowntoken.RemoveAll(x => x.cooldownuntil <= DateTime.Now); // remove obsolete cooldown
            if (cooldowntoken.Any(x => x.token == token && x.cooldownuntil > DateTime.Now))
            {
                return BadRequest(new { status = "Error", message = "You are on cooldown. Please wait before sending another play request." });
            }
            var user = _context.Users.FirstOrDefault(u => u.Token == token);
            if (user == null)
            {
                return Unauthorized(new { status = "Error", message = "Invalid token." });
            }

            var song = _context.Songs.Include(s => s.Album).ThenInclude(x => x.Artist).Include(x => x.SongCounter).FirstOrDefault(s => s.Id == songId);
            if (song == null || song.SongCounter == null)
            {
                return NotFound(new { status = "Error", message = "Song not found." });
            }
            song.SongCounter.TotalPlayed += 1;
            _context.SaveChanges();
            cooldowntoken.Add((DateTime.Now.AddMinutes(2), user.Token)); // 2 minutes cooldown
            return Ok();
        }
    }
}
