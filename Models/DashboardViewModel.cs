namespace Project2EmailNight.Models
{
    public class DashboardViewModel
    {
        public int TotalMessages { get; set; }     // sistem toplam
        public int InboxCount { get; set; }        // benim gelen
        public int SentCount { get; set; }         // benim giden
        public int UnreadCount { get; set; }       // benim okunmamış
        public int ReadCount { get; set; }         // benim okunan
        public int MyTotalCount { get; set; }      // Inbox + Sent
        public int CategoryCount { get; set; }
        public int UserCount { get; set; }
        public List<CategoryCountItem> CategoryCounts { get; set; } = new();
//nestedı kaldırdın unutma dashboaard controller kontrol et
        public class CategoryCountItem
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public string? ColorHex { get; set; }
            public int Count { get; set; }
        }

    }
}
