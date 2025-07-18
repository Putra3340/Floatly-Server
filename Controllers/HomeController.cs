using Microsoft.AspNetCore.Mvc;

namespace Floaty_Music.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
