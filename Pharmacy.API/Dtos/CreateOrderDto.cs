namespace Pharmacy.API.Dtos
{
    public class CreateOrderDto
    {
        public decimal Amount { get; set; }
        public string Address { get; set; }
        public int DeliveryManId { get; set; }
        public int ClientId { get; set; }
    }
}
