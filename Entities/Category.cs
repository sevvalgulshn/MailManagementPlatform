namespace Project2EmailNight.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;

        public string? ColorHex { get; set; }   

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

}
