using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvcFinal2.Models;

namespace mvcFinal2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly mvcFinal2.Data.AppDbContext _context;

        public AdminController(mvcFinal2.Data.AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var listings = await _context.Listings
                .Include(l => l.User)
                .OrderBy(l => l.IsApproved)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();

            var model = new mvcFinal2.ViewModels.AdminDashboardViewModel
            {
                Listings = listings
            };
            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> SuspendListing(int listingId)
        {
            var listing = await _context.Listings.FindAsync(listingId);
            if (listing != null)
            {
                listing.IsSuspended = true;
                // Also unapprove it so it doesn't show up
                listing.IsApproved = false; 
                await _context.SaveChangesAsync();
            }
            return RedirectRequest(Request.Headers["Referer"].ToString());
        }

        private IActionResult RedirectRequest(string url)
        {
            if(string.IsNullOrEmpty(url)) return RedirectToAction("Index");
            return Redirect(url);
        }

        [HttpPost]
        public IActionResult Approve(int id)
        {
            var listing = _context.Listings.Find(id);
            if (listing != null)
            {
                listing.IsApproved = true;
                listing.IsSuspended = false; // Unsuspend if manually approved
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var listing = _context.Listings.Find(id);
            if (listing != null)
            {
                _context.Listings.Remove(listing);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
