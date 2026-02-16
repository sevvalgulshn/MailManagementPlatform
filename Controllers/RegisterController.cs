using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;
using Project2EmailNight.Models;
using System.Security.Cryptography;

namespace Project2EmailNight.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailSettings _emailSettings;

        // ✅ appsettings'ten EmailSettings okuyacağız
        public RegisterController(UserManager<AppUser> userManager, IOptions<EmailSettings> emailOptions)
        {
            _userManager = userManager;
            _emailSettings = emailOptions.Value;
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new UserRegisterDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (dto.TermsAcceptedStr != "true")
            {
                ModelState.AddModelError(nameof(dto.TermsAcceptedStr), "Şartlar ve Koşulları kabul etmelisiniz.");
                return View(dto);
            }

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
            {
                if (!existing.EmailConfirmed)
                {
                    var code2 = Generate6DigitCode();
                    existing.ConfirmCode = code2;
                    await _userManager.UpdateAsync(existing);

                    TrySendConfirmMail(existing.Email!, code2);
                    TempData["Info"] = "Bu email zaten kayıtlı. Yeni doğrulama kodu gönderildi.";

                    return RedirectToAction("VerifyEmail", new { email = existing.Email });
                }

                ModelState.AddModelError("", "Bu email ile zaten kayıt olunmuş. Giriş yapabilirsiniz.");
                return View(dto);
            }

            var appUser = new AppUser
            {
                Name = dto.Name,
                Surname = dto.Surname,
                Email = dto.Email,
                UserName = dto.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(appUser, dto.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(dto);
            }

            var code = Generate6DigitCode();
            appUser.ConfirmCode = code;
            await _userManager.UpdateAsync(appUser);

            TrySendConfirmMail(appUser.Email!, code);

            return RedirectToAction("VerifyEmail", new { email = appUser.Email });
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string email, string confirmCode)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                ViewBag.Email = email;
                return View();
            }

            if (string.IsNullOrWhiteSpace(confirmCode) || user.ConfirmCode != confirmCode)
            {
                ModelState.AddModelError("", "Doğrulama kodu hatalı.");
                ViewBag.Email = email;
                return View();
            }

            user.EmailConfirmed = true;
            user.ConfirmCode = null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Email doğrulandı. Giriş yapabilirsiniz.";
            return RedirectToAction("UserLogin", "Login", new { email = user.Email });
        }

        private static string Generate6DigitCode()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return value.ToString("D6");
        }

        private void TrySendConfirmMail(string toEmail, string code)
        {
            try
            {
                SendConfirmMail(toEmail, code);
            }
            catch (Exception ex)
            {
                TempData["MailError"] = "Mail gönderilemedi: " + ex.Message;
            }
        }

        // her şey appsettings'ten
        private void SendConfirmMail(string toEmail, string code)
        {
            // Basit güvenlik
            if (string.IsNullOrWhiteSpace(_emailSettings.FromEmail) || string.IsNullOrWhiteSpace(_emailSettings.Password))
                throw new InvalidOperationException("EmailSettings.FromEmail veya EmailSettings.Password boş. appsettings.Development.json kontrol et.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("User", toEmail));
            message.Subject = "Email Doğrulama Kodu";
            message.Body = new BodyBuilder
            {
                TextBody = $"Doğrulama kodunuz: {code}"
            }.ToMessageBody();

            using var client = new SmtpClient();

            client.Connect(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
            client.Authenticate(_emailSettings.FromEmail, _emailSettings.Password);

            client.Send(message);
            client.Disconnect(true);
        }
    }
}
