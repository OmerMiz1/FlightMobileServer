using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlightMobileServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ScreenshotController : ControllerBase
    {
        // Todo return jpg image from server.
        [HttpGet]
        public async Task<IActionResult> GetScreenshot() {
            throw new NotImplementedException();
        }
    }
}
