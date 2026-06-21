using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(20)]
        public string Role { get; set; } // 'Admin' ou 'Cliente'

        [MaxLength(12)]
        public string ExternalId { get; set; }

        [MaxLength(64)]
        public string EmailAlternativo { get; set; }
    }
}
