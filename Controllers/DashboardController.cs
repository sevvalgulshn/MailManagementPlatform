using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project2EmailNight.Context;
using Project2EmailNight.Entities;
using Project2EmailNight.Models;
using static Project2EmailNight.Models.DashboardViewModel;

namespace Project2EmailNight.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            if (user == null) return Challenge();

            var inboxQuery = _context.Messages.Where(x => x.ReceiverEmail == user.Email);
            var sentQuery = _context.Messages.Where(x => x.SenderEmail == user.Email);

            var inboxCount = await inboxQuery.CountAsync();
            var sentCount = await sentQuery.CountAsync();
            var unreadCount = await inboxQuery.CountAsync(x => x.IsStatus == false);
            var readCount = await inboxQuery.CountAsync(x => x.IsStatus == true);

            var model = new DashboardViewModel
            {
                InboxCount = inboxCount,
                SentCount = sentCount,
                UnreadCount = unreadCount,
                ReadCount = readCount,
                MyTotalCount = inboxCount + sentCount,

                TotalMessages = await _context.Messages.CountAsync(),
                CategoryCount = await _context.Categories.CountAsync(),
                UserCount = await _context.Users.CountAsync()
            };

            model.CategoryCounts = await _context.Messages
                .Where(m => m.CategoryId != null)
                .GroupBy(m => new
                {
                    CategoryId = m.CategoryId!.Value,
                    m.Category!.CategoryName,
                    m.Category.ColorHex
                })
                .Select(g => new CategoryCountItem
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ColorHex = g.Key.ColorHex,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return View(model);
        }
    }
}
