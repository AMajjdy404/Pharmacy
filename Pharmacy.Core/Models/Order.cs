using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pharmacy.Core.Models
{
    public enum OrderStatus
    {
        Pending,
        InDelivery,
        Delivered,
        Canceled
    }
    public class Order
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Address { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public int DeliveryManId { get; set; } //FK
        public int ClientId { get; set; } //FK
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public double Distance { get; set; } = 0;
        public DeliveryMan DeliveryMan { get; set; }
        public Client Client { get; set; }
    }
}
