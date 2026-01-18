using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvcFinal2.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad gereklidir.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        public string Password { get; set; } = string.Empty;

        public string? University { get; set; }
        public string? City { get; set; }
        
        public string UserType { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; } // Keeping for backward compatibility or external links
        public byte[]? ProfileImage { get; set; } // Storing image directly in DB

        public string Role { get; set; } = "User";


        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        
        [InverseProperty("Buyer")]
        public ICollection<Listing> Purchases { get; set; } = new List<Listing>();

        [InverseProperty("ReviewedUser")]
        public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();

        public double AverageRating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
    }
}
