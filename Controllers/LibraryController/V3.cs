using Floaty_Music.Models;
using Floaty_Music.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Floaty_Music.Controllers
{
    // V3 - Youtube Music Library API
    [Route("api/library/v3")]
    [ApiController]
    public class LibraryV3Controller : ControllerBase
    {
        private readonly FloatlyContext _context;
        public LibraryV3Controller(FloatlyContext cont)
        {
            _context = cont;
        }
        
        [HttpGet("search")] // search
        public async Task<IActionResult> Search([FromQuery] string? anycontent)
        {
            var list = await YoutubeService.SearchAsync(anycontent);

            foreach (var item in list)
            {
                Console.WriteLine($"{item.Title} — {item.Url}");
            }

            return Ok(list);
        }

        [HttpGet("{yturl}")]
        public async Task<IActionResult> GetSong(string yturl)
        {
            var streamurl = await YoutubeService.StreamAudioAsync(yturl);
            return Ok(streamurl);
        }
        
    }
}
