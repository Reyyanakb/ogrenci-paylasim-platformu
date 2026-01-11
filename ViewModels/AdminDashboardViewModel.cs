using System.Collections.Generic;
using mvcFinal2.Models;

namespace mvcFinal2.ViewModels
{
    public class AdminDashboardViewModel
    {
        public List<Listing> Listings { get; set; } = new List<Listing>();

    }
}
