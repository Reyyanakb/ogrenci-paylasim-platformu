using System.Collections.Generic;

namespace mvcFinal2.Models
{
    public class ListingsViewModel
    {
        public List<Listing> Rooms { get; set; } = new List<Listing>();
        public List<Listing> Items { get; set; } = new List<Listing>();
    }
}
