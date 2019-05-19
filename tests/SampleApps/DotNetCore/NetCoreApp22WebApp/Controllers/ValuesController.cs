using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NetCoreApp22WebApp.Controllers
{
    [Route("/")]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "Hello World!";
        }

        [HttpGet("executingDir")]
        public ActionResult<string> ExecutingDir()
        {
            return $"App is running from directory: {Directory.GetCurrentDirectory()}";
        }
    }
}
