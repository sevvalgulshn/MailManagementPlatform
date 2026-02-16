using System.ComponentModel.DataAnnotations;

namespace Project2EmailNight.Dtos
{
    public class UserLoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool IsPersistent { get; set; } = true; // Beni hatırla
    }
}
