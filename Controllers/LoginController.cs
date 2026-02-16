using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;

namespace Project2EmailNight.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult UserLogin(string? email)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Inbox", "Message");

            // VerifyEmail'den gelince email kutusu dolu gelsin
            ViewBag.PrefillEmail = email;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);


            AppUser? user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                user = await _userManager.FindByNameAsync(dto.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email/Kullanıcı adı veya şifre hatalı.");
                return View(dto);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Lütfen emailinizi doğrulayın.");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,          
                dto.Password,
                dto.IsPersistent,
                lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Dashboard");

            ModelState.AddModelError("", "Email/Kullanıcı adı veya şifre hatalı.");
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("UserLogin", "Login");
        }
    }
}
