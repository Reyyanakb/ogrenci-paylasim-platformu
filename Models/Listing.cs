using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvcFinal2.Models
{
    public class Listing
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string Location { get; set; } = string.Empty;

        // "Room" or "Item"
        public string Type { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;
        public bool IsSuspended { get; set; } = false;

        public bool IsCompleted { get; set; } = false;
        
        // "Sold" or "Rented"
        public ListingCompletionType? CompletedType { get; set; }

        // Foreign Key
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        public int? BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        public AppUser? Buyer { get; set; }
    }

    public enum ListingCompletionType
    {
        Sold,
        Rented
    }
}
