using FlightMobileWeb.ClientModels;
using FlightMobileWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightMobileWeb.Controllers
{
    [ApiController]
    public class FlightGearController : ControllerBase
    {
        private readonly IAsyncTcpClient _client;
        private readonly string _screenshotUrl;

        public FlightGearController(IAsyncTcpClient client, SimulatorConfig config)
        {
            _client = client;
            _screenshotUrl = $"http://{config.Ip}:{config.HttpPort}/screenshot";
        }

        [Route("api/command")]
        [HttpPost]
        public async Task<IActionResult> UpdateCommand([FromBody] Command cmd)
        {
            Result result;
            try
            {
                result = await _client.Execute(cmd);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            if (result == Result.Ok)
            {
                return Ok();
            }

            return BadRequest();
        }

        [Route("screenshot")]
        [HttpGet]
        public async Task<IActionResult> GetScreenshot()
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            var response = await httpClient.GetByteArrayAsync(_screenshotUrl);

            /* Failed to get image from simulator */
            if (response == null)
                return BadRequest();

            return File(response, "image/jpeg");
        }
    }
}
