using Microsoft.AspNetCore.Mvc;
using mvcFinal2.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace mvcFinal2.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        public IActionResult Details(int id)
        {
            var user = _context.Users
                .Include(u => u.Listings)
                .Include(u => u.ReviewsReceived)
                .ThenInclude(r => r.Reviewer)
                .FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
    }
}
