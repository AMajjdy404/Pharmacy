using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pharmacy.Core.Models
{
    public class DeliveryMan
    {
        public int Id { get; set; }
        public string  Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsAvaliable { get; set; } = false;
        [JsonIgnore]
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
