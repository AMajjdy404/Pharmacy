namespace Pharmacy.API.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Address { get; set; }
        public string DeliveryMan { get; set; }
        public string Client { get; set; }
        public string OrderStatus { get; set; }
        public double Distance { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
