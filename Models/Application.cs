using OpenIddict.EntityFrameworkCore.Models;
using System.Collections.Generic;


namespace AuthServer.Models;

public class Application : OpenIddictEntityFrameworkCoreApplication<string, Authorization, Token>
{
    public List<string> CustomPermissions { get; set; } = [];
}


