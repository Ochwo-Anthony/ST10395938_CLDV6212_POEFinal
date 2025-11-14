namespace ABCRetailers.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string CustomerUsername { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
