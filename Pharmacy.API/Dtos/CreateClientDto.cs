using System.ComponentModel.DataAnnotations;

namespace Pharmacy.API.Dtos
{
    public class CreateClientDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }
    }
}
