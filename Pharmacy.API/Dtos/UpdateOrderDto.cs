using Pharmacy.Core.Models;

namespace Pharmacy.API.Dtos
{
    public class UpdateOrderDto
    {
        public decimal Amount { get; set; }
        public string Address { get; set; }
        public int ClientId { get; set; }
        public int DeliveryManId { get; set; }
    }
}
