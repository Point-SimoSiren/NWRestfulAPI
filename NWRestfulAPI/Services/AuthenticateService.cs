using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NWRestfulAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NWRestfulAPI.Services
{
    public class AuthenticateService : Interfaces.IAuthenticateService
    {

        private readonly NorthwindOriginalContext db = new NorthwindOriginalContext();

        private readonly AppSettings _appSettings;

        // Konstruktori
        public AuthenticateService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }


        public LoggedUser Authenticate(string username, string password)
        {
            var foundUser = db.Users.SingleOrDefault(x => x.Username == username && x.Password == password);

            // Jos ei käyttäjää löydy palautetaan null
            if (foundUser == null)
            {
                return null;
            }

            // Jos käyttäjä löytyy:
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, foundUser.UserId.ToString()),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(ClaimTypes.Version, "V3.1")
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Montako päivää token on voimassa

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            LoggedUser loggedUser = new LoggedUser();

            loggedUser.Username = foundUser.Username;
            loggedUser.Accesslevel = foundUser.Acceslevel;
            loggedUser.Token = tokenHandler.WriteToken(token);

            return loggedUser; // Palautetaan kutsuvalle controllerimetodille
        }
    }
}


