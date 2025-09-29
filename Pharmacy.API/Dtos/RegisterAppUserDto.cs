using System.ComponentModel.DataAnnotations;

namespace Pharmacy.API.Dtos
{
    public class RegisterAppUserDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; } 
    }
}
