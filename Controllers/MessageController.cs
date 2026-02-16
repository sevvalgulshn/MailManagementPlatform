using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project2EmailNight.Context;
using Project2EmailNight.Entities;
using Microsoft.AspNetCore.Http;
using System.IO;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.RegularExpressions;




namespace Project2EmailNight.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MessageController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            var text = Regex.Replace(html, "<.*?>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(text);
        }


        // Dropdown'u dolduran helper (GET ve POST'ta tekrar kullanacağız!!!!!!!!
        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(x => x.CategoryName)
                .Select(x => new SelectListItem
                {
                    Value = x.CategoryId.ToString(),
                    Text = x.CategoryName
                })
                .ToListAsync();
        }
        private void SendMailViaGmail(string receiverEmail, string subject, string messageDetail)
        {
            var mimeMessage = new MimeMessage();

            var mailboxAddressFrom = new MailboxAddress("Şevval", "sevvalgulsahin44@gmail.com");
            mimeMessage.From.Add(mailboxAddressFrom);

            var mailboxAddressTo = new MailboxAddress("User", receiverEmail);
            mimeMessage.To.Add(mailboxAddressTo);

            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = messageDetail, // CKEditor HTML üretiyor
                TextBody = StripHtml(messageDetail) // opsiyonel ama iyi
            };


            mimeMessage.Body = bodyBuilder.ToMessageBody();


            using var smtpClient = new SmtpClient();

            smtpClient.CheckCertificateRevocation = false;
            smtpClient.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            smtpClient.Authenticate("sevvalgulsahin44@gmail.com", "ptpl oyqp ppfg jeos");

            smtpClient.Send(mimeMessage);
            smtpClient.Disconnect(true);
        }


        [HttpGet]
        public async Task<IActionResult> CreateMessage()
        {
            await LoadCategoriesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessage(Message message)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return Challenge();

           
            // validasyon
            if (string.IsNullOrWhiteSpace(message.ReceiverEmail))
                ModelState.AddModelError(nameof(message.ReceiverEmail), "Alıcı email zorunlu.");

            if (string.IsNullOrWhiteSpace(message.Subject))
                ModelState.AddModelError(nameof(message.Subject), "Konu zorunlu.");

            if (string.IsNullOrWhiteSpace(message.MessageDetail))
                ModelState.AddModelError(nameof(message.MessageDetail), "Mesaj zorunlu.");

            if (message.CategoryId == null)
                ModelState.AddModelError(nameof(message.CategoryId), "Kategori seçmelisiniz.");


            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(message);
            }

            // Server-side set
            message.SenderEmail = user.Email!;
            message.SendDate = DateTime.Now;

            // Başta false (mail gitmezse kayıt kaybolmasın)
            message.IsStatus = false;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

           
            try
            {
                SendMailViaGmail(message.ReceiverEmail, message.Subject, message.MessageDetail);

                message.IsStatus = true;
                _context.Messages.Update(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Mesaj kaydedildi ve e-posta gönderildi.";
            }
            catch (Exception ex)
            {
                // Kayıt DB'de durur
                TempData["Error"] = "Mesaj kaydedildi ama e-posta gönderilemedi: " + ex.Message;
            }

            return RedirectToAction("SendBox");
        }

        public async Task<IActionResult> Inbox(int? categoryId)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return Challenge();

            var query = _context.Messages
                .Where(x => x.ReceiverEmail == user.Email);

            if (categoryId.HasValue)
                query = query.Where(x => x.CategoryId == categoryId.Value);

            var messageList = await query
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(messageList);
        }



        public async Task<IActionResult> SendBox()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return Challenge();

            var messageList = await _context.Messages
                .Where(x => x.SenderEmail == user.Email)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(messageList);
        }

        // hem gelen hem giden açılsın
        public async Task<IActionResult> MessageDetail(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return Challenge();

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id &&
                (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email));

            if (message == null) return NotFound();

            // Okundu: 
            if (message.ReceiverEmail == user.Email && message.IsStatus == false)
            {
                message.IsStatus = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        public async Task<IActionResult> MessageDelete(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return Challenge();

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id &&
                (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email));

            if (message == null) return NotFound();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(message.SenderEmail == user.Email ? "SendBox" : "Inbox");
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload, IFormFile file)
        {
            var incoming = upload ?? file;

            if (incoming == null || incoming.Length == 0)
                return BadRequest("Dosya gelmedi (upload/file null).");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(incoming.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await incoming.CopyToAsync(stream);
            }

            // CKEditor 5 içn
            return Json(new { url = "/uploads/" + fileName });
        }



    }
}
