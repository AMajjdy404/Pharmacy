using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pharmacy.Core.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string Token { get; set; }

        // Owner Info
        public string OwnerId { get; set; }
        public string UserType { get; set; } 

        // Validity
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // Revocation
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
    }


}
