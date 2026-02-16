namespace Project2EmailNight.Models
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string FromName { get; set; } = "Project2EmailNight";
        public string FromEmail { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
