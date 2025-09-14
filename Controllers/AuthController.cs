using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Floaty_Music.Controllers
{
    [Route("auth")]
    public partial class AuthController : Controller
    {
        [HttpPost("login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            if (username == GlobalConfiguration.ADMIN_USERNAME && password == GlobalConfiguration.ADMIN_PASSWORD)
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

                var identity = new ClaimsIdentity(claims, "MyAuth");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("MyAuth", principal);

                return RedirectToAction("Dashboard", "Song");
            }

            return Unauthorized("Invalid credentials");
        }

        [HttpGet("logout")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyAuth");
            return RedirectToAction("Login");
        }
        [HttpGet("login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult LoginView()
        {
            return View("Login");
        }
    }
}
