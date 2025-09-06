using Floaty_Music.Models;
using Floaty_Music.Service;
using Floaty_Music.Utils;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Floaty_Music.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly FloatlyContext _context;
        public AuthController(FloatlyContext context)
        {
            _context = context;
        }
        #region Website
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            if (username == GlobalConfiguration.ADMIN_USERNAME && password == GlobalConfiguration.ADMIN_PASSWORD)
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

                var identity = new ClaimsIdentity(claims, "MyCookie");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("MyCookie", principal);

                return RedirectToAction("Dashboard", "Song");
            }

            return Unauthorized("Invalid credentials");
        }

        [HttpGet("logout")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookie");
            return RedirectToAction("Login");
        }
        [HttpGet("login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult LoginView()
        {
            return View("Login");
        }
        #endregion
        #region Desktop Auth
        [HttpPost("desktop/login")]
        public async Task<IActionResult> LoginDesktop([FromForm] string username, [FromForm] string password)
        {
            if (username.IsNullOrEmpty() || password.IsNullOrEmpty())
                return Unauthorized();
            var user = _context.Users.FirstOrDefault(x => x.Username == username);
            if (user == null)
                return Unauthorized(new { Message = "User not found" });

            // verify password
            bool valid = Argon2.Verify(user.PasswordHash, password);
            if (!valid)
                return Unauthorized(new { Message = "Invalid password" });
            return Ok(user);
        }
        [HttpPost("desktop/autologin")]
        public async Task<IActionResult> LoginDesktopToken([FromForm] string token)
        {
            if (token.IsNullOrEmpty())
                return Unauthorized();
            var user = _context.Users.FirstOrDefault(x => x.Token == token);
            if (user == null)
                return Unauthorized(new {Message = "Invalid Token" });
            return Ok(new {user});
        }
        [HttpPost("desktop/register")]
        public async Task<IActionResult> RegisterDesktop([FromForm] string username, [FromForm] string email, [FromForm] string password)
        {
            if(username.IsNullOrEmpty() || email.IsNullOrEmpty() || password.IsNullOrEmpty())
                return BadRequest("Invalid data");
            var verified = _context.VerifiedEmail.FirstOrDefault(x => x.Email == email);
            if(verified == null)
                return Unauthorized("Email not verified");
            var existingUser = _context.Users.FirstOrDefault(x => x.Username == username || x.Email == email);
            if(existingUser != null)
                return Conflict("Username or Email already exists");
            string hashedpass = HashHelper.Argon2Hash(password);
            string token = HashHelper.GenerateLoginToken(); // save like a session token
            _context.Users.Add( new Users
            {
                Username = username,
                Email = email,
                PasswordHash = hashedpass,
                Token = token,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Ok(new {Message = "Account Successfully Created",Token = token});
        }
        [HttpPost("desktop/verify-email")]
        public async Task<IActionResult> VerifyEmail([FromForm] string email)
        {
            if (email.IsNullOrEmpty())
                return BadRequest("Email is required");
            var existingUser = _context.Users.FirstOrDefault(x => x.Email == email);
            if (existingUser != null)
                return Conflict("Email already in use");
            var alreadyVerified = _context.VerifiedEmail.FirstOrDefault(x => x.Email == email);
            if (alreadyVerified != null)
                return Conflict("Email already verified");
            // This will send a verification url to the user's email
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(email));
            string verify_url = $"{Request.Scheme}://{Request.Host}/auth/desktop/verify-token?token={token}";
            EmailService emailservice = new EmailService();
            await emailservice.SendEmailAsync(email, "Verify your email",
                $"        <!-- Heading -->\r\n        <h1 class=\"h3 fw-bold text-dark mb-2\">Verify Your Email</h1>\r\n        \r\n        <!-- Description -->\r\n        <p class=\"text-muted mb-4 lh-base\">\r\n            Please click the button below to verify your email address and complete your account setup.\r\n        </p>\r\n\r\n        <!-- Email Address -->\r\n        <div class=\"bg-light rounded p-3 mb-4\">\r\n            <p class=\"small text-muted mb-1\">Verifying email for:</p>\r\n            <p class=\"fw-medium text-dark mb-0\">{email}</p>\r\n        </div>\r\n\r\n        <!-- Verify Button -->\r\n        <a href=\"{verify_url}\" class=\"btn btn-primary btn-lg mb-4\" style=\"border-color: #3466F2;font-size: large;\">\r\n            Verify Email</a>\r\n\r\n        <!-- Security Note -->\r\n        <div class=\"mt-4 pt-4 border-top\">\r\n            <p class=\"small text-muted lh-base mb-0\">\r\n                This verification link will NOT expire. If you did not create an account, please ignore this email.\r\n            </p>\r\n        </div>"


                );
            return Ok();
        }
        [HttpGet("desktop/verify-token")]
        public async Task<IActionResult> VerifyToken([FromQuery] string token)
        {
            if (!HashHelper.TryDecodeBase64(token, out var email) || string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Invalid or malformed token" });
            if (!string.IsNullOrWhiteSpace(email))
            {
                _context.VerifiedEmail.Add(new Models.VerifiedEmail
                {
                    Email = email,
                    VerifiedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return Ok(new { email = email });
            }
            return Unauthorized("Invalid or expired token");
        }
        #endregion
    }

}
