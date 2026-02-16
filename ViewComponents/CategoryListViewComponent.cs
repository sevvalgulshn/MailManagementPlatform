using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project2EmailNight.Context;

namespace Project2EmailNight.ViewComponents
{
    public class CategoryListViewComponent : ViewComponent
    {
        private readonly EmailContext _context;

        public CategoryListViewComponent(EmailContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var values = _context.Categories.ToList();
            return View(values);
        }
    }

}
