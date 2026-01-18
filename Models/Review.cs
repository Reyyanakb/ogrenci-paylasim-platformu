using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvcFinal2.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int ListingId { get; set; }
        [ForeignKey("ListingId")]
        public Listing? Listing { get; set; }

        public int ReviewerId { get; set; }
        [ForeignKey("ReviewerId")]
        public AppUser? Reviewer { get; set; }

        public int ReviewedUserId { get; set; }
        [ForeignKey("ReviewedUserId")]
        public AppUser? ReviewedUser { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(500)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
