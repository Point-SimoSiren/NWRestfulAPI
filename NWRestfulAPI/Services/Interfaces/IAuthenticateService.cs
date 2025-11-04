using NWRestfulAPI.Models;

namespace NWRestfulAPI.Services.Interfaces
{
    public interface IAuthenticateService
    {
        LoggedUser Authenticate(string username, string password);
    }
}
