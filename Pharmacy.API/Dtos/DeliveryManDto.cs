namespace Pharmacy.API.Dtos
{
    public class DeliveryManDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsAvaliable { get; set; } = false;
    }
}
