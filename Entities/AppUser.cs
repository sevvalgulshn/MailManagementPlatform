using Microsoft.AspNetCore.Identity;

namespace Project2EmailNight.Entities
{
    public class AppUser: IdentityUser
    {

        public string Name { get; set; }
        public string Surname { get; set; }
        public string? ImageUrl { get; set; }
        public string? About { get; set; }
        public string? ConfirmCode { get; set; }
        //public string? ConfirmCode { get; set; } ---> bunu yapacaksın mig de yap registercontroller içinde ConfirmCode="123456" eklenecek.
        //bu kod mail olarak da gitmeli, email confirmed durumu dbde true olmalı, kullanıcının e mail confirmedı doğru değilse giriş yapmamalııı onaylayın lütfen mesajı görünsün

    }
}
