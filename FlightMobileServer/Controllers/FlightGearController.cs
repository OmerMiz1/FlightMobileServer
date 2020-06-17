using System;
using System.Collections.Generic;
using System.Linq;
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
        private IAsyncTcpClient _client;
        private IConfiguration _config;

        public FlightGearController(IAsyncTcpClient client, IConfiguration config) {
            _client = client;
            _config = config;
        }

        [Route("api/command")]
        [HttpPost]
        public async Task<IActionResult> UpdateCommand([FromBody] Command cmd) {
            Result result;
            try {
                result = await _client.Execute(cmd);
            }
            catch (Exception e) {
                throw new NotImplementedException(); // todo handle exception
            }

            if (result == Result.Ok) return Ok();
            return BadRequest();
        }

        [Route("screenshot")]
        [HttpGet]
        public async Task<IActionResult> GetScreenshot() {
            return Redirect(
                $"http://{_config["SimulatorHttpIp"]}:{_config["SimulatorHttpPort"]}/screenshot");
        }
    }
}
