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
        

        
    }
}
