using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SenAnAPI.HostedServices;

namespace SenAnAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SenAnController : ControllerBase
    {
        private readonly SenAnHostedService _TimedHostedService;

        public SenAnController(SenAnHostedService timedHostedService)
        {
            _TimedHostedService = timedHostedService;
        }

        [HttpGet]
        public string GetResult([FromQuery(Name = "word")] string singleWord)
        {
            return this._TimedHostedService.ReadData(singleWord);
        }
    }
}