namespace NWRestfulAPI.Models
{
    public class LoggedUser
    {
        public string Username { get; set; }
        public int Accesslevel { get; set; }
        public string? Token { get; set; }

    }
}
