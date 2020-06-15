using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightMobileServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlightMobileServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandController : ControllerBase
    {

        //POST /api/command
        [HttpPost]
        public async Task<IActionResult> PostCommand([FromBody] Command cmd) {
            throw new NotImplementedException();
        }
    }
}
