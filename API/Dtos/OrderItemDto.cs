namespace API.Dtos
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string PictureUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        
        // --- ADAUGĂ ASTEA DOUĂ ---
        public string ProducerId { get; set; }
        public string ProducerName { get; set; }
    }
}