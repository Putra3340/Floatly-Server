using Floaty_Music.Models;
using Floaty_Music.Service;
using Floaty_Music.Utils;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Floaty_Music.Controllers.ClientController
{
    [Route("auth")]
    public partial class AuthController : Controller
    {
        private static List<(DateTime expiredtime,string email,string verifytoken)> emailverifyreq = new(); // temp store email verify request
        private static HashSet<string> emailverified = new(); // temp store email verified, so user can register if server restarts they will be lost, doesnt affect login
        private readonly FloatlyContext _context;
        public AuthController(FloatlyContext context)
        {
            _context = context;
        }

        [HttpPost("desktop/login")]
        public async Task<IActionResult> LoginDesktop([FromForm] string username, [FromForm] string password) // username or email
        {
            if (username.IsNullOrEmpty() || password.IsNullOrEmpty())
                return Unauthorized();
            var user = _context.Users.FirstOrDefault(x => x.Username == username || x.Email == username);
            if (user == null)
                return Unauthorized(new { Message = "User not found" });

            // verify password
            bool valid = Argon2.Verify(user.PasswordHash, password);
            if (!valid)
                return Unauthorized(new { Message = "Invalid password" });
            user.Token = HashHelper.GenerateLoginToken();
            await _context.SaveChangesAsync();
            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                token = user.Token,
                createdAt = user.CreatedAt
            });
        }
        [HttpPost("desktop/autologin")]
        public async Task<IActionResult> LoginDesktopToken([FromForm] string token)
        {
            if (token.IsNullOrEmpty())
                return Unauthorized();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null)
                return Unauthorized(new { Message = "Invalid Token" });
            // check if expired return 
            string exp = string.Empty;
            if (!HashHelper.TryDecodeBase64(user.Token, out exp))
                return BadRequest(new { message = "Invalid or malformed token" });
            if (exp.IsNullOrEmpty())
                return BadRequest(new { message = "Invalid or malformed token" });
            exp = exp.Split("|").Last();
            if (long.TryParse(exp, out long epoch))
            {
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;

                if (dateTime <= DateTime.UtcNow)
                    return Unauthorized("Token Expired, Please Login Again");
            }
            else
            {
                return Unauthorized("Token Part is Invalid");
            }
            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                token = user.Token,
                createdAt = user.CreatedAt
            });
        }
        [HttpPost("desktop/register")]
        public async Task<IActionResult> RegisterDesktop([FromForm] string username, [FromForm] string email, [FromForm] string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return BadRequest("Invalid data");
            // check if email was verified
            if (!emailverified.Contains(email))
                return Unauthorized("Email not verified");
            var existingUser = _context.Users.FirstOrDefault(x => x.Username == username || x.Email == email);
            if (existingUser != null)
                return Conflict("Username or Email already exists");
            string hashedpass = HashHelper.Argon2Hash(password);
            string token = HashHelper.GenerateLoginToken(); // save like a session token
            _context.Users.Add(new Users
            {
                Username = username,
                Email = email,
                PasswordHash = hashedpass,
                Token = token,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Account Successfully Created", Token = token });
        }
        [HttpPost("desktop/verify-email")]
        public async Task<IActionResult> VerifyEmail([FromForm] string email)
        {
            if (email.IsNullOrEmpty())
                return BadRequest("Email is required");
            var existingUser = _context.Users.FirstOrDefault(x => x.Email == email);
            if (existingUser != null)
                return Conflict("Email already in use");
            emailverifyreq.RemoveAll(x => x.expiredtime <= DateTime.Now); // remove expired requests
            var alreadyRequested = emailverifyreq.FirstOrDefault(x => x.email == email && x.expiredtime > DateTime.Now);
            if (alreadyRequested != default)
                return Conflict("Verification already requested, wait a few minutes to request new verification link");
            // This will send a verification url to the user's email (random token)
            string token = HashHelper.GenerateRandomLongToken();
            string verify_url = $"{Request.Scheme}://{Request.Host}/auth/desktop/verify-token?token={token}";
            emailverifyreq.Add((DateTime.Now.AddMinutes(5), email, token)); // expires in 5 minutes, after 5 minutes user need to request again
            EmailService emailservice = new EmailService();
            await emailservice.SendEmailAsync(email, "Verify your email",
                $"        <!-- Heading -->\r\n        <h1 class=\"h3 fw-bold text-dark mb-2\">Verify Your Email</h1>\r\n        \r\n        <!-- Description -->\r\n        <p class=\"text-muted mb-4 lh-base\">\r\n            Please click the button below to verify your email address and complete your account setup.\r\n        </p>\r\n\r\n        <!-- Email Address -->\r\n        <div class=\"bg-light rounded p-3 mb-4\">\r\n            <p class=\"small text-muted mb-1\">Verifying email for:</p>\r\n            <p class=\"fw-medium text-dark mb-0\">{email}</p>\r\n        </div>\r\n\r\n        <!-- Verify Button -->\r\n        <a href=\"{verify_url}\" class=\"btn btn-primary btn-lg mb-4\" style=\"border-color: #3466F2;font-size: large;\">\r\n            Verify Email</a>\r\n\r\n        <!-- Security Note -->\r\n        <div class=\"mt-4 pt-4 border-top\">\r\n            <p class=\"small text-muted lh-base mb-0\">\r\n                This verification link will expired in 5 minutes. If you did not create an account, please ignore this email.\r\n            </p>\r\n        </div>"


                );
            return Ok("Verification link sent");
        }
        [HttpGet("desktop/verify-token")]
        public IActionResult VerifyToken([FromQuery] string token)
        {
            // cleanup expired requests first
            emailverifyreq.RemoveAll(x => x.expiredtime <= DateTime.UtcNow);

            var match = emailverifyreq.FirstOrDefault(x => x.verifytoken == token);
            if (match == default)
                return Unauthorized("Invalid or expired token");

            if (!emailverified.Contains(match.email))
                emailverified.Add(match.email);

            emailverifyreq.Remove(match);

            return Ok(new { email = match.email,message = "Your account has been verified!" });
        }


        //[HttpPost("desktop/reset-password")]
        //public async Task<IActionResult> RequestReset([FromQuery] string email)
        //{
        //    if (email.IsNullOrEmpty())
        //        return BadRequest();
        //    var user = _context.Users.FirstOrDefault(x => x.Email == email);
        //    if (user == null)
        //        return BadRequest("Email is not registered");
        //    string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(email));
        //    string verify_url = $"{Request.Scheme}://{Request.Host}/auth/desktop/verify-token?token={token}";
        //    EmailService emailservice = new EmailService();
        //    await emailservice.SendEmailAsync(email, "Reset your Password",
        //        $"        <!-- Heading -->\r\n        <h1 class=\"h3 fw-bold text-dark mb-2\">Reset Password</h1>\r\n        \r\n        <!-- Description -->\r\n        <p class=\"text-muted mb-4 lh-base\">\r\n            Please click the button below to reset your password.\r\n        </p>\r\n\r\n        <!-- Email Address -->\r\n        <div class=\"bg-light rounded p-3 mb-4\">\r\n            <p class=\"small text-muted mb-1\">Verifying email for:</p>\r\n            <p class=\"fw-medium text-dark mb-0\">{email}</p>\r\n        </div>\r\n\r\n        <!-- Verify Button -->\r\n        <a href=\"{verify_url}\" class=\"btn btn-primary btn-lg mb-4\" style=\"border-color: #3466F2;font-size: large;\">\r\n            Reset Password</a>\r\n\r\n        <!-- Security Note -->\r\n        <div class=\"mt-4 pt-4 border-top\">\r\n            <p class=\"small text-muted lh-base mb-0\">\r\n                This verification link will expire in 1 days. If you did not request this, please ignore this email.\r\n            </p>\r\n        </div>"


        //        );
        //    return Ok();
        //}
    }
}
