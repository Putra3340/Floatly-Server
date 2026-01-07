using Floaty_Music.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Floaty_Music.Controllers.SongController;

namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly FloatlyContext _context;
        public UserController(FloatlyContext db)
        {
            _context = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int start = 1, int end = 10)
        {
            // BIG TODO: Pagination
            // CUTTED
            var users = await _context.Users.Select(x =>
            new
            {
                x.Id,
                x.Username,
                x.Email,
                x.PremiumExpired,
            }
            ).ToListAsync();
            return Json(users);
        }
        [HttpPost]
        public async Task<IActionResult> SetRole(int id,int role)
        {
            var user = await _context.Users.Where(x => x.Id == id).FirstOrDefaultAsync();
            user.PremiumExpired = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
