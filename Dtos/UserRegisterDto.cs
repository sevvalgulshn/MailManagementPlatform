using System.ComponentModel.DataAnnotations;

namespace Project2EmailNight.Dtos
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Email adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "Şifre en az 8 karakter, 1 büyük harf, 1 küçük harf, 1 rakam ve 1 özel karakter içermelidir.")]
        public string Password { get; set; }

        // checkbox işaretlenirse "true" gelir, işaretlenmezse null gelir!!!!!!!
        [Required(ErrorMessage = "Şartlar ve Koşullar kabul edilmelidir.")]
        public string TermsAcceptedStr { get; set; }
    }
}
