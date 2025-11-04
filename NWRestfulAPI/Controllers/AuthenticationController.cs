using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NWRestfulAPI.Models;
using NWRestfulAPI.Services.Interfaces;

namespace NWRestfulAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {

        private IAuthenticateService _authenticateService;

        public AuthenticationController(IAuthenticateService authenticateService)
        {
            _authenticateService = authenticateService;
        }

        // Tähän tulee Front endin kirjautumisyritys
        [HttpPost]
        public ActionResult Post([FromBody] Credentials tunnukset)
        {
            // Kutsutaan autentikointipalvelua
            var loggedUser = _authenticateService.Authenticate(tunnukset.Username, tunnukset.Password);

            if (loggedUser == null)
                return BadRequest("Käyttäjätunus tai salasana on virheellinen");

            return Ok(loggedUser); // Palautus front endiin (sis. vain loggedUser luokan mukaiset kentät)
        }
    }
}
