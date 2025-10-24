using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NWRestfulAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelloController : ControllerBase
    {

        [HttpGet]
        public ActionResult SayHello()
        {
            return Ok("Hello, World!");
        }

    }
}
