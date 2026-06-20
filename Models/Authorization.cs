using OpenIddict.EntityFrameworkCore.Models;

namespace AuthServer.Models;

public class Authorization : OpenIddictEntityFrameworkCoreAuthorization<string, Application, Token>
{
}