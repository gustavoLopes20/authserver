using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(20)]
    public string Role { get; set; } // 'Admin' ou 'Cliente'

    [MaxLength(12)]
    public string ExternalId { get; set; }

    [MaxLength(64)]
    public string EmailAlternativo { get; set; }

    [MaxLength(64)]
    public string NomePessoa { get; set; }

    public ICollection<UserApplication> AllowedApplications { get; set; } = [];
}

public class UserApplication
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    // O valor aqui deve bater com o ClientId do OpenIddict (ex: "rentainvestApp-idd")
    public string ClientId { get; set; }
}
