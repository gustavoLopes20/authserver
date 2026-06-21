using AuthServer.Infra;
using AuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthServer.Config
{
    public class DataSeeder(RoleManager<IdentityRole> roleManager, 
        UserManager<ApplicationUser> userManager, IOpenIddictApplicationManager applicationManager)
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IOpenIddictApplicationManager _applicationManager = applicationManager;

        public async Task SeedOpenIddictAsync()
        {
            if (await _applicationManager.FindByClientIdAsync("selfInvestApp-idd") == null)
            {
                await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "selfInvestApp-idd",
                    ClientSecret = "bc6ff4afbf56e5184734f7b8de50143b0e90318ba6ea4382587301b6e3701097",
                    DisplayName = "SelfInvest App",
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Scopes.OpenId, // Escopo OpenID (literal)
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Roles,
                    },
                    
                });
            }

            if (await _applicationManager.FindByClientIdAsync("rentainvestApp-idd") == null)
            {
                await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "rentainvestApp-idd",
                    ClientSecret = "a2f0590c8848fcd3eaea9060cc30c9029689debf32b689c1c78ba862635b3ba5",
                    DisplayName = "Rentainvest Sistema",
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Scopes.OpenId, // Escopo OpenID (literal)
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Roles,
                    }
                });
            }

            if (await _applicationManager.FindByClientIdAsync("internationalRentainvestApp-idd") == null)
            {
                await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "internationalRentainvestApp-idd",
                    ClientSecret = "ecd786ce976ef9d9be1a82618c26a49341469f6aab16bd232d8a9499f71baaf4",
                    DisplayName = "International Rentainvest",
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Scopes.OpenId, // Escopo OpenID (literal)
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Roles,
                    }
                });
            }

            if (await _applicationManager.FindByClientIdAsync("clientPainel_app") == null)
            {
                await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "clientPainel_app",
                    ClientSecret = "bc6ff4afbf56e5184734f7b8de50143b0e90318ba6ea4382587301b6e3701097",
                    DisplayName = "Rentainvest Painel",
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Scopes.OpenId, // Escopo OpenID (literal)
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Roles,
                    }
                });
            }
        }




        public async Task Initialize()
        {
            string[] roleNames = ["Admin", "Consultor", "Client", "SelfInvest", "Api", "Developer"];

            // Criação de roles se não existirem
            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Criar usuário Admin
            await SeedAdminUserAsync("gustavol17@outlook.com", "u509%(lCl<l2!",  "Admin");
            await SeedAdminUserAsync("luizpa30@gmail.com", "LV8@=u[sQ3$4", "Admin");
            await SeedAdminUserAsync("selfinvest@rentainvest.com.br", "LV8@=u[sQ3$4", "SelfInvest");
        }

        private async Task SeedAdminUserAsync(string adminEmail, string pass, string role, string clienteRID = "")
        {
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new ApplicationUser { UserName = adminEmail, Email = adminEmail, Role = role, ExternalId = clienteRID };
                var result = await _userManager.CreateAsync(user, pass);

         
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);

                    // Adiciona uma claim customizada com um ID customizado
                    var claim = new Claim("ClientRID", user.ExternalId);
                    await _userManager.AddClaimAsync(user, claim);
                }
            }
        }


    }
}
