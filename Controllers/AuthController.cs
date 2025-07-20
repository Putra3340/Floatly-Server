using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Floaty_Music.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            if (username == "admin" && password == "69420")
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

                var identity = new ClaimsIdentity(claims, "MyCookie");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("MyCookie", principal);

                return RedirectToAction("Index", "Song");
            }

            return Unauthorized("Invalid credentials");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookie");
            return Ok("Logged out");
        }
        [HttpGet("login")]
        public IActionResult LoginView()
        {
            return View("Login");
        }

    }

}
