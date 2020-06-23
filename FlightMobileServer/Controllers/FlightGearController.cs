using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FlightMobileServer.ClientModels;
using FlightMobileServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlightMobileServer.Controllers
{
    [ApiController]
    public class FlightGearController : ControllerBase
    {
        private readonly IAsyncTcpClient _client;
        private readonly string _screenshotUrl;

        public FlightGearController(IAsyncTcpClient client, SimulatorConfig config) {
            _client = client;
            _screenshotUrl = $"http://{config.Ip}:{config.HttpPort}/screenshot";
        }

        [Route("api/command")]
        [HttpPost]
        public async Task<IActionResult> UpdateCommand([FromBody] Command cmd) {
            Result result;
            try {
                result = await _client.Execute(cmd);
            }
            catch (Exception) {
                return BadRequest();
            }

            if (result == Result.Ok) {
                Debug.WriteLine("Ok");
                return Ok();
            }

            Debug.WriteLine("BadRequest");
            return BadRequest();
        }

        [Route("screenshot")]
        [HttpGet]
        public async Task<IActionResult> GetScreenshot() {
            var httpClient = new HttpClient {
                Timeout = TimeSpan.FromSeconds(30)
            };
            var responseMessage = await httpClient.GetAsync(_screenshotUrl);
            var resultImage = await responseMessage.Content.ReadAsByteArrayAsync();
            return File(resultImage, "image/jpg");
        }

        //debug remove
        [Route("test")]
        [HttpGet]
        public async Task<IActionResult> Test() { 
            return Ok("Hello");
        }
    }
}
