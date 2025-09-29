namespace Pharmacy.API.Dtos
{
    public class InDeliveryOrderDto
    {
        public int OrderId { get; set; }
        public string ClientName { get; set; }
        public string DeliveryManName { get; set; }
        public decimal Amount { get; set; }
        public string Address { get; set; }
    }

}
