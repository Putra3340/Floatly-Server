using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Controllers
{
    public class LikeController : ControllerBase
    {
        private readonly FloatlyContext _context;
        public LikeController(FloatlyContext cont)
        {
            _context = cont;
        }
        [HttpPost("api/likes")]
        public async Task<IActionResult> GetLikes([FromForm] string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var likes = await _context.Likes
                .Where(x => x.UserId == user.Id)
                .Select(x => new { x.SongId, x.CreatedAt })
                .ToListAsync();

            return Ok(likes);
        }

        [HttpPost("api/likesong")]
        public async Task<IActionResult> LikeSong([FromForm] string token, [FromForm] int SongId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (SongId <= 0) return BadRequest("Invalid data");

            if (await _context.Likes.AnyAsync(x => x.UserId == user.Id && x.SongId == SongId))
                return Conflict("Already liked");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var model = new Likes
                {
                    SongId = SongId,
                    UserId = user.Id,
                    CreatedAt = DateTime.Now,
                };
                _context.Likes.Add(model);

                var songcounter = await _context.SongCounter.FirstOrDefaultAsync(x => x.SongId == SongId);
                if (songcounter == null) return Forbid("Song is already deleted");

                songcounter.TotalLikes++;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "Liked successfully" });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        [HttpPost("api/unlikesong")]
        public async Task<IActionResult> UnlikeSong([FromForm] string token, [FromForm] int SongId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (SongId <= 0) return BadRequest("Invalid data");

            var like = await _context.Likes.FirstOrDefaultAsync(x => x.UserId == user.Id && x.SongId == SongId);
            if (like == null) return NotFound("Like not found");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Likes.Remove(like);

                var songcounter = await _context.SongCounter.FirstOrDefaultAsync(x => x.SongId == SongId);
                if (songcounter == null) return Forbid("Song is already deleted");

                if (songcounter.TotalLikes > 0)
                    songcounter.TotalLikes--;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "Unliked successfully" });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
