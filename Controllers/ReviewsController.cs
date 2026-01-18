using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvcFinal2.Data;
using mvcFinal2.Models;
using System.Security.Claims;

namespace mvcFinal2.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int listingId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdStr);

            var listing = await _context.Listings
                .Include(l => l.User)
                .Include(l => l.Buyer)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return NotFound();

            // Validate permission
            if (listing.BuyerId != currentUserId)
            {
                return Forbid(); // Only buyer can review
            }

            if (!listing.IsCompleted)
            {
                TempData["Error"] = "Bu ilan henüz tamamlanmamış.";
                return RedirectToAction("Details", "Listings", new { id = listingId });
            }

            // Check if already reviewed
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ListingId == listingId && r.ReviewerId == currentUserId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Bu ilan için daha önce değerlendirme yaptınız.";
                return RedirectToAction("Index", "Dashboard");
            }

            var review = new Review
            {
                ListingId = listingId,
                ReviewedUserId = listing.UserId,
                ReviewerId = currentUserId
            };

            ViewBag.ListingTitle = listing.Title;
            ViewBag.SellerName = listing.User?.FullName;

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            if (!ModelState.IsValid)
            {
                 // Repopulate ViewBags if validation fails
                 var list = await _context.Listings.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == review.ListingId);
                 if(list != null) {
                    ViewBag.ListingTitle = list.Title;
                    ViewBag.SellerName = list.User?.FullName;
                 }
                return View(review);
            }

            // Double check validation to prevent bypassing client side
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            int currentUserId = int.Parse(userIdStr!);
            
            // Re-verify eligibility (Listing is completed, user is buyer, not reviewed yet)
             bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ListingId == review.ListingId && r.ReviewerId == currentUserId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Bu ilan için daha önce değerlendirme yaptınız.";
                return RedirectToAction("Index", "Dashboard");
            }
            
            review.CreatedAt = DateTime.Now;
            review.ReviewerId = currentUserId; // Ensure it's the logged in user

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update Seller Stats automatically
            await UpdateSellerStats(review.ReviewedUserId);

            // Send Notification (System Message) to Seller
            var listing = await _context.Listings.FindAsync(review.ListingId);
            if(listing != null) {
                var message = new Message
                {
                    SenderId = currentUserId, // Reviewer
                    ReceiverId = review.ReviewedUserId, // Seller
                    Content = $"Sistem Mesajı: <strong>{listing.Title}</strong> ilanınız için yeni bir değerlendirme aldınız!<br>Puan: {review.Rating} Yıldız<br>Yorum: {review.Comment}",
                    SentAt = DateTime.Now,
                    IsRead = false,
                    IsSystemMessage = true
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Değerlendirmeniz başarıyla kaydedildi.";
            return RedirectToAction("Index", "Dashboard"); // Return to dashboard
        }

        private async Task UpdateSellerStats(int sellerId)
        {
            var seller = await _context.Users.FindAsync(sellerId);
            if (seller != null)
            {
                var stats = await _context.Reviews
                    .Where(r => r.ReviewedUserId == sellerId)
                    .GroupBy(r => r.ReviewedUserId)
                    .Select(g => new { Average = g.Average(r => r.Rating), Count = g.Count() })
                    .FirstOrDefaultAsync();

                if (stats != null)
                {
                    seller.AverageRating = stats.Average;
                    seller.ReviewCount = stats.Count;
                }
                else
                {
                    seller.AverageRating = 0;
                    seller.ReviewCount = 0;
                }
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
