using Microsoft.AspNetCore.Mvc;

namespace Floaty_Music.Controllers
{
    [ApiController]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet("Weather")]
        public IActionResult Weather()
        {
            var weather = new
            {
                Date = DateTime.Now,
                TemperatureC = 25,
                Summary = "Sunny"
            };
            return Ok(weather);
        }
    }
}
