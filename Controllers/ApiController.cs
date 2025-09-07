using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Floaty_Music.Controllers
{
    public class ApiController : Controller
    {
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
        [HttpPost("api/likesong")]
        public async Task<IActionResult> LikeSong([FromForm] string token, [FromForm]int SongId)
        {
            var user = _context.Users.FirstOrDefault(x => x.Token == token);
            if (user == null)
                return Unauthorized("Invalid Token");
            if (user.Id <= 0 || SongId <= 0)
                return BadRequest("Invalid data");

            
            // Check if already liked
            var exists = await _context.Likes.FirstOrDefaultAsync(x => x.UserId == user.Id && x.SongId == SongId);

            var model = new Likes
            {
                SongId = SongId,
                UserId = user.Id,
                CreatedAt = DateTime.Now,
            };  
            if (exists != null)
                return Conflict("Already liked");

            _context.Likes.Add(model);
            await _context.SaveChangesAsync();
            var songcounter = _context.SongCounter.Find(model.SongId);
            if(songcounter == null)
            {
                return Forbid("Song is already deleted");
            }
            songcounter.TotalLikes++; // add 1
            await _context.SaveChangesAsync();
            return Ok(new { message = "Liked successfully" });
        }
        [HttpPost("api/unlikesong")]
        public async Task<IActionResult> UnlikeSong([FromForm] string token, [FromForm] int SongId) // for security we use token
        {
            var user = _context.Users.FirstOrDefault(x => x.Token == token);
            if (user == null)
                return Unauthorized("Invalid Token");
            if (user.Id <= 0 || SongId <= 0)
                return BadRequest("Invalid data");
            
            var like = await _context.Likes.FirstOrDefaultAsync(x => x.UserId == user.Id && x.SongId == SongId);
            if (like == null)
                return NotFound("Like not found");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            var songcounter = await _context.SongCounter.FindAsync(SongId);
            if (songcounter == null)
                return Forbid("Song is already deleted");

            if (songcounter.TotalLikes > 0)
                songcounter.TotalLikes--; // decrement properly

            await _context.SaveChangesAsync();

            return Ok(new { message = "Unliked successfully" });
        }

    }
}
