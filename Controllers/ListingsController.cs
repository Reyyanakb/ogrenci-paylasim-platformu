using Microsoft.AspNetCore.Mvc;
using mvcFinal2.Data;
using mvcFinal2.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace mvcFinal2.Controllers
{
    public class ListingsController : Controller
    {
        private readonly AppDbContext _context;

        public ListingsController(AppDbContext context)
        {
            _context = context;
        }

        private async Task SeedListings()
        {
            if (!_context.Listings.Any())
            {
                var user = _context.Users.FirstOrDefault();
                if (user != null)
                {
                    _context.Listings.AddRange(
                        new Listing
                        {
                            Title = "Beşiktaş'ta Öğrenci Evi",
                            Description = "Evimiz 3+1 olup, bir odamız boştur. Gelen arkadaşın kendi odası olacak. Mutfak ve banyo ortaktır.",
                            Price = 5500,
                            Location = "Beşiktaş, İstanbul",
                            Type = "Room",
                            ImageUrl = "https://via.placeholder.com/800x450",
                            IsApproved = true,
                            UserId = user.Id
                        },
                         new Listing
                         {
                             Title = "Kadıköy'de Ferah Oda",
                             Description = "Metroya 5 dakika yürüme mesafesinde, geniş ve güneş alan oda.",
                             Price = 6000,
                             Location = "Kadıköy, İstanbul",
                             Type = "Room",
                             ImageUrl = "https://via.placeholder.com/800x450/333",
                             UserId = user.Id
                         },
                         new Listing
                         {
                             Title = "Çalışma Masası",
                             Description = "Az kullanılmış, sağlam çalışma masası. Taşınma nedeniyle satılık.",
                             Price = 500,
                             Location = "Şişli, İstanbul",
                             Type = "Item",
                             ImageUrl = "https://via.placeholder.com/800x450/444",
                             UserId = user.Id
                         },
                         new Listing
                         {
                             Title = "Üsküdar'da Eşyalı Oda",
                             Description = "Marmaray'a yakın, merkezi konumda. Faturalar dahil.",
                             Price = 7000,
                             Location = "Üsküdar, İstanbul",
                             Type = "Room",
                             ImageUrl = "https://via.placeholder.com/800x450/555",
                             UserId = user.Id
                         }
                    );
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            await SeedListings();
            var rooms = await _context.Listings
                .Where(l => l.Type == "Room" && l.IsApproved)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var items = await _context.Listings
                .Where(l => l.Type == "Item" && l.IsApproved)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var model = new ListingsViewModel
            {
                Rooms = rooms,
                Items = items
            };

            return View(model);
        }

        public async Task<IActionResult> Rooms()
        {
            await SeedListings();
            var rooms = await _context.Listings.Where(l => l.Type == "Room" && l.IsApproved).ToListAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Items()
        {
            await SeedListings();
            var items = await _context.Listings.Where(l => l.Type == "Item" && l.IsApproved).ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var listing = await _context.Listings.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
            if (listing == null)
            {
                return NotFound();
            }
            return View(listing);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int currentUserId = int.Parse(userIdStr);

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
            {
                return NotFound();
            }

            if (listing.IsCompleted)
            {
                TempData["Error"] = "Bu ilan zaten işlem görmüş.";
                return RedirectToAction("Details", new { id = listing.Id });
            }

            // Update Listing Status
            listing.IsCompleted = true;
            listing.CompletedType = listing.Type == "Room" ? ListingCompletionType.Rented : ListingCompletionType.Sold;
            listing.BuyerId = currentUserId;
            
            _context.Listings.Update(listing);

            // Send System Message to Owner
            // Don't send if owner is marking their own listing (if that flow is allowed, though prompt implies buyer action)
            if (listing.UserId != currentUserId)
            {
                var buyer = await _context.Users.FindAsync(currentUserId);
                string actionText = listing.Type == "Room" ? "kiralandı" : "satın alındı";
                
                var message = new Message
                {
                    SenderId = currentUserId, // Sender is the buyer
                    ReceiverId = listing.UserId, // Receiver is the owner
                    Content = $"Sistem Mesajı: İlanınız ({listing.Title}) bu kullanıcı tarafından {actionText} olarak işaretlendi. <br> <a href='/Listings/Details/{listing.Id}' class='text-primary fw-bold text-decoration-none'>İlanı Görüntüle</a>",
                    SentAt = DateTime.Now,
                    IsRead = false,
                    IsSystemMessage = true
                };
                _context.Messages.Add(message);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "İlan durumu güncellendi.";
            return RedirectToAction("Details", new { id = listing.Id });
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateListing(int id)
        {
             var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int currentUserId = int.Parse(userIdStr);

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
            {
                return NotFound();
            }

            // Only owner can reactivate
            if (listing.UserId != currentUserId)
            {
                return Unauthorized();
            }

            listing.IsCompleted = false;
            listing.CompletedType = null;

            _context.Listings.Update(listing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "İlan tekrar yayına alındı.";
            return RedirectToAction("Details", new { id = listing.Id }); // Or Dashboard
        }
    }
}
