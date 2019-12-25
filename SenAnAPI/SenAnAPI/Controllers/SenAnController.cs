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

        public class ProcessFulltextParam
        {
            public string fulltext { get; set; }
        }

        [HttpPost]
        public string ProcessFulltext([FromBody] ProcessFulltextParam param1)
        {
            if (param1 != null && !string.IsNullOrEmpty(param1.fulltext) && !string.IsNullOrWhiteSpace(param1.fulltext))
                return this._TimedHostedService.ReadData(param1.fulltext);
            else
                return "";
        }
    }
}