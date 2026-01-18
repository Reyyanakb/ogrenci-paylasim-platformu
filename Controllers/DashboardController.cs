using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mvcFinal2.ViewModels;
using mvcFinal2.Models;
using System.Linq;
using System.Security.Claims;

namespace mvcFinal2.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly mvcFinal2.Data.AppDbContext _context;

        public DashboardController(mvcFinal2.Data.AppDbContext context)
        {
            _context = context;
        }

        private async Task<int> GetUnreadCount(int userId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();
        }



        public async Task<IActionResult> Messages(int? receiverId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                // Get conversations logic
                var messageUserIds = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                var conversations = await _context.Users
                    .Where(u => messageUserIds.Contains(u.Id))
                    .ToListAsync();

                // If sender/receiver specific log exists (e.g. starting a new chat) but not in list yet, add them manually
                // This handles the "Send Message" button from Listing Details
                if (receiverId.HasValue && !conversations.Any(u => u.Id == receiverId.Value))
                {
                    var newChatUser = await _context.Users.FindAsync(receiverId.Value);
                    if (newChatUser != null)
                    {
                        conversations.Insert(0, newChatUser);
                    }
                }

                var currentUser = await _context.Users.FindAsync(userId);

                var viewModel = new DashboardViewModel
                {
                    User = currentUser,
                    Conversations = conversations,
                    UnreadMessageCount = await GetUnreadCount(userId),
                    ActivePage = "Messages",
                    CurrentUserId = userId
                };

                // If a specific chat is selected
                if (receiverId.HasValue)
                {
                    var receiver = await _context.Users.FindAsync(receiverId.Value);
                    if (receiver != null)
                    {
                        viewModel.CurrentReceiver = receiver;

                        // Load messages
                        var messages = await _context.Messages
                            .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId.Value) ||
                                        (m.SenderId == receiverId.Value && m.ReceiverId == userId))
                            .OrderBy(m => m.SentAt)
                            .ToListAsync();

                        viewModel.CurrentChatMessages = messages;

                        // Mark as read
                        var unreadMessages = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
                        if (unreadMessages.Any())
                        {
                            foreach (var msg in unreadMessages)
                            {
                                msg.IsRead = true;
                            }
                            await _context.SaveChangesAsync();
                            // Re-fetch count
                            viewModel.UnreadMessageCount = await GetUnreadCount(userId);
                        }
                    }
                }

                return View(viewModel);
            }
             return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Messages", new { receiverId });
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int currentUserId))
            {
                var message = new mvcFinal2.Models.Message
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return RedirectToAction("Messages", new { receiverId });
            }
             return RedirectToAction("Index");
        }

        public IActionResult CreateListing()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateListing(mvcFinal2.Models.Listing model, IFormFile imageFile)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                 return RedirectToAction("Index");
            }

            // We must remove ImageUrl from ModelState validation because it's not bound from the form
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                model.UserId = userId;
                model.CreatedAt = DateTime.Now;

                // Handle file upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    model.ImageUrl = "/uploads/" + uniqueFileName;
                }
                else
                {
                    model.ImageUrl = "https://via.placeholder.com/800x450";
                }

                _context.Listings.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public async Task<IActionResult> EditListing(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
                if (listing == null)
                {
                    return NotFound();
                }
                return View(listing);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EditListing(int id, mvcFinal2.Models.Listing model, IFormFile? imageFile)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                if (id != model.Id)
                {
                    return NotFound();
                }

                var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
                if (listing == null)
                {
                    return NotFound();
                }

                // Remove ImageUrl from validation as it is not mandatory to change it
                ModelState.Remove("ImageUrl");
                ModelState.Remove("User"); // Avoid validation error for navigation property

                if (ModelState.IsValid)
                {
                    listing.Title = model.Title;
                    listing.Description = model.Description;
                    listing.Price = model.Price;
                    listing.Location = model.Location;
                    listing.Type = model.Type;
                    // We don't update CreatedAt, UserId, IsApproved, IsSuspended here normally, unless we want to unapprove after edit?
                    // For now let's keep status.

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        listing.ImageUrl = "/uploads/" + uniqueFileName;
                    }

                    _context.Listings.Update(listing);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "İlan başarıyla güncellendi.";
                    return RedirectToAction("Index");
                }
                return View(model);
            }
             return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
                if (listing != null)
                {
                    _context.Listings.Remove(listing);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "İlan silindi.";
                }
            }
            return RedirectToAction("Index");
        }
        public async Task<int> GetFavoritesCount(int userId)
        {
            return await _context.Favorites.CountAsync(f => f.UserId == userId);
        }



        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users
                    .Include(u => u.ReviewsReceived)
                        .ThenInclude(r => r.Reviewer)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                var userListings = await _context.Listings
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                var viewModel = new DashboardViewModel
                {
                    User = user,
                    Listings = userListings,
                    UnreadMessageCount = await GetUnreadCount(userId),
                    FavoritesCount = await GetFavoritesCount(userId),

                    ActivePage = "Index",
                    CurrentUserId = userId
                };
                return View(viewModel);
            }
            return View(new DashboardViewModel());
        }



        public async Task<IActionResult> Favorites()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                var favoriteListings = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Listing)
                    .Where(f => f.Listing != null)
                    .Select(f => f.Listing!)
                    .ToListAsync();

                var viewModel = new DashboardViewModel
                {
                    User = user,
                    Favorites = favoriteListings,
                    UnreadMessageCount = await GetUnreadCount(userId),
                    FavoritesCount = favoriteListings.Count,
                    ActivePage = "Favorites"
                };
                return View(viewModel);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Purchases()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                
                var purchases = await _context.Listings
                    .Where(l => l.BuyerId == userId)
                    .Include(l => l.User)
                    .ToListAsync();
                
                var viewModel = new DashboardViewModel
                {
                    User = user,
                    Purchases = purchases,
                    UnreadMessageCount = await GetUnreadCount(userId),
                    FavoritesCount = await GetFavoritesCount(userId),
                    ActivePage = "Purchases"
                };
                return View(viewModel);
            }
             return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int listingId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId);

                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                }
                else
                {
                    var favorite = new mvcFinal2.Models.Favorite
                    {
                        UserId = userId,
                        ListingId = listingId
                    };
                    _context.Favorites.Add(favorite);
                }
                await _context.SaveChangesAsync();
            }
            // Return to referrer or default
            return Redirect(Request.Headers["Referer"].ToString() ?? "/Dashboard");
        }

        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                
                var viewModel = new DashboardViewModel
                {
                    User = user,
                    UnreadMessageCount = await GetUnreadCount(userId),
                    FavoritesCount = await GetFavoritesCount(userId),

                    ActivePage = "Profile"
                };
                return View(viewModel);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(DashboardViewModel model, IFormFile? profileImage)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(model.User?.FullName))
                    {
                        user.FullName = model.User.FullName;
                    }
                    user.University = model.User?.University;
                    user.City = model.User?.City;
                    
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await profileImage.CopyToAsync(memoryStream);
                            user.ProfileImage = memoryStream.ToArray();
                        }
                        
                        // Clear the URL to prefer the blob or just leave it? 
                        // Let's clear it so we know to use the blob, or keep it as backup?
                        // If we are "saving to database", we should probably use the BLOB.
                        // But for now, let's just null out the URL so views switch to BLOB.
                        user.ProfileImageUrl = null;
                    }

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Profiliniz güncellendi.";
                    return RedirectToAction("Profile");
                }
            }
            return RedirectToAction("Index");
        }
    }
}
