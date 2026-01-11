using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvcFinal2.Models
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        public int ListingId { get; set; }
        [ForeignKey("ListingId")]
        public Listing? Listing { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
