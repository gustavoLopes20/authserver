using OpenIddict.EntityFrameworkCore.Models;
using System.Net;

namespace AuthServer.Models;

public class Token : OpenIddictEntityFrameworkCoreToken<string, Application, Authorization>
{
}