using System.Collections.Generic;
using mvcFinal2.Models;

namespace mvcFinal2.ViewModels
{
    public class DashboardViewModel
    {
        public AppUser? User { get; set; }
        public List<Listing> Listings { get; set; } = new List<Listing>();
        public List<AppUser> Conversations { get; set; } = new List<AppUser>();
        public int UnreadMessageCount { get; set; }
        public string ActivePage { get; set; } = "Index";

        // Properties for active chat view
        public List<Message> CurrentChatMessages { get; set; } = new List<Message>();
        public AppUser? CurrentReceiver { get; set; }
        public int CurrentUserId { get; set; }

        public List<Listing> Favorites { get; set; } = new List<Listing>();
        public int FavoritesCount { get; set; }


    }
}
