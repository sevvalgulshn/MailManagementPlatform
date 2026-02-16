using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;          // CountAsync / ToListAsync / FirstOrDefaultAsync
using Project2EmailNight.Context;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;
using System.Globalization;
using System.Text.Json;
namespace Project2EmailNight.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailContext _context;

        public ProfileController(UserManager<AppUser> userManager, EmailContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Profile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return RedirectToAction("UserLogin", "Login");

            var dto = new UserEditDto
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                ImageUrl = user.ImageUrl
            };

            return View(dto);
        }

        // POST: /Profile/Index
        [HttpPost]
        public async Task<IActionResult> Index(UserEditDto dto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return RedirectToAction("UserLogin", "Login");

            user.Name = dto.Name;
            user.Surname = dto.Surname;
            user.Email = dto.Email;

            // Şifre boşsa değiştirme
            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.Password);

            // Resim seçildiyse upload
            if (dto.Image != null && dto.Image.Length > 0)
            {
                var ext = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("", "Sadece .jpg, .jpeg, .png, .webp yükleyebilirsiniz.");
                    return View(dto);
                }

                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                Directory.CreateDirectory(imagesFolder);

                var fileName = Guid.NewGuid().ToString("N") + ext;
                var savePath = Path.Combine(imagesFolder, fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await dto.Image.CopyToAsync(stream);

                user.ImageUrl = fileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return RedirectToAction("Index");

            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);

            return View(dto);
        }

        // Dashboard: /Profile/AdminProfile
        [HttpGet]
        public async Task<IActionResult> AdminProfile()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return RedirectToAction("UserLogin", "Login");

            var userMail = user.Email!;
            var now = DateTime.Now;

            // Bu ay aralığı
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            // Bu ay gelen / giden
            ViewBag.IncomingCount = await _context.Messages.CountAsync(x =>
                x.ReceiverEmail == userMail &&
                x.SendDate >= monthStart && x.SendDate < nextMonthStart);

            ViewBag.OutgoingCount = await _context.Messages.CountAsync(x =>
                x.SenderEmail == userMail &&
                x.SendDate >= monthStart && x.SendDate < nextMonthStart);

            // Okunmayan: IsStatus=false
            ViewBag.UnreadCount = await _context.Messages.CountAsync(x =>
                x.ReceiverEmail == userMail &&
                x.IsStatus == false);

            ViewBag.UnreadInfo = ((int)(ViewBag.UnreadCount ?? 0)) > 0 ? "Needs attention" : "All clear";

            // Bu ay en çok mail atan kişi
            var topContact = await _context.Messages
                .Where(x =>
                    x.ReceiverEmail == userMail &&
                    x.SendDate >= monthStart && x.SendDate < nextMonthStart)
                .GroupBy(x => x.SenderEmail)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            ViewBag.TopContact = string.IsNullOrWhiteSpace(topContact) ? "No contacts yet" : topContact;

            // Son 12 ay trafik dataset'i
            var twelveMonthsAgo = now.AddMonths(-12).Date;

            var trafficMessages = await _context.Messages
                .Where(x =>
                    x.SendDate >= twelveMonthsAgo &&
                    (x.ReceiverEmail == userMail || x.SenderEmail == userMail))
                .Select(x => new { x.SendDate, x.ReceiverEmail, x.SenderEmail })
                .ToListAsync();

            bool IsReceived(string receiver) => string.Equals(receiver, userMail, StringComparison.OrdinalIgnoreCase);
            bool IsSent(string sender) => string.Equals(sender, userMail, StringComparison.OrdinalIgnoreCase);

            // Daily (son 30 gün)
            var dayPoints = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-29 + i))
                .ToList();

            var dailyLabels = dayPoints.Select(d => d.ToString("dd MMM", CultureInfo.InvariantCulture)).ToList();
            var dailyReceived = dayPoints.Select(d => trafficMessages.Count(m => IsReceived(m.ReceiverEmail) && m.SendDate.Date == d)).ToList();
            var dailySent = dayPoints.Select(d => trafficMessages.Count(m => IsSent(m.SenderEmail) && m.SendDate.Date == d)).ToList();

            // Weekly (son 12 hafta)
            static DateTime StartOfWeekMonday(DateTime dt)
            {
                int diff = (7 + (int)dt.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                return dt.Date.AddDays(-diff);
            }

            var weekStarts = Enumerable.Range(0, 12)
                .Select(i => StartOfWeekMonday(now.Date).AddDays(-7 * (11 - i)))
                .ToList();

            var weeklyLabels = weekStarts.Select(w => w.ToString("dd MMM", CultureInfo.InvariantCulture)).ToList();
            var weeklyReceived = weekStarts.Select(ws => trafficMessages.Count(m =>
                IsReceived(m.ReceiverEmail) && m.SendDate.Date >= ws && m.SendDate.Date < ws.AddDays(7))).ToList();
            var weeklySent = weekStarts.Select(ws => trafficMessages.Count(m =>
                IsSent(m.SenderEmail) && m.SendDate.Date >= ws && m.SendDate.Date < ws.AddDays(7))).ToList();

            // Monthly (son 12 ay)
            var monthStarts = Enumerable.Range(0, 12)
                .Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-(11 - i)))
                .ToList();

            var monthlyLabels = monthStarts.Select(ms => ms.ToString("MMM yyyy", CultureInfo.InvariantCulture)).ToList();
            var monthlyReceived = monthStarts.Select(ms => trafficMessages.Count(m =>
                IsReceived(m.ReceiverEmail) && m.SendDate >= ms && m.SendDate < ms.AddMonths(1))).ToList();
            var monthlySent = monthStarts.Select(ms => trafficMessages.Count(m =>
                IsSent(m.SenderEmail) && m.SendDate >= ms && m.SendDate < ms.AddMonths(1))).ToList();

            ViewBag.TrafficDaily = JsonSerializer.Serialize(new { labels = dailyLabels, received = dailyReceived, sent = dailySent });
            ViewBag.TrafficWeekly = JsonSerializer.Serialize(new { labels = weeklyLabels, received = weeklyReceived, sent = weeklySent });
            ViewBag.TrafficMonthly = JsonSerializer.Serialize(new { labels = monthlyLabels, received = monthlyReceived, sent = monthlySent });

            return View(user); // Views/Profile/AdminProfile.cshtml
        }
    }
}
