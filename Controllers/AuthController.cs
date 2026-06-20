using AuthServer.Dtos;
using AuthServer.Models;
using AuthServer.Services.Helpers;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<ApplicationUser> userManager,
     SignInManager<ApplicationUser> signInManager, IEmailService emailService) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly IEmailService _emailService = emailService;

        [HttpGet("list-usrs")]
        public  List<ApplicationUser> GetUrs([FromQuery] string id)
        {
            try
            {
                var users = _userManager.Users.ToList();

                var user = users.FirstOrDefault(a => a.Id == id);

                string[] roleNames = ["Admin", "Consultor", "Client", "SelfInvest", "Api", "Developer"];

                if (user != null)
                {
                    if (user.Role == "Admin")
                    {
                        roleNames = ["Api", "Developer"];

                        return users.Where(a => a.Id != id && !roleNames.Contains(a.Role)).ToList();
                    }
                    return [];
                }
                return [];

            }catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            try
            {
                var adminUser = await _userManager.FindByEmailAsync(model.Email.Trim());

                if (adminUser != null)
                    await _userManager.DeleteAsync(adminUser);
                
                var user = new ApplicationUser 
                { 
                    UserName = model.Email.Trim(), 
                    Email = model.Email.Trim(), 
                    Role = model.Role.Trim(), 
                    ExternalId = model.ExternalReferenceId.Trim()
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    return Ok(new RegisterResponseDto { Id = user.Id });
                }

                var stringBuilder = new StringBuilder();

                foreach (var error in result.Errors)
                {
                    stringBuilder.AppendLine($"Code: {error.Code}, Description: {error.Description}");
                }

                string errors = stringBuilder.ToString();

                return BadRequest(new RegisterResponseDto { Errors = errors, Success = false });

            }catch(Exception ex)
            {
                return BadRequest(new RegisterResponseDto { Errors = ex.Message, Success = false });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            try
            {
                // Encontre o usuário pelo ID
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound("Usuário não encontrado.");
                }

                // Tente alterar a senha
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    return Ok("Senha alterada com sucesso.");
                }

                // Retorne erros, se houver
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("O request do OpenID Connect não pôde ser recuperado.");

            if (request.IsPasswordGrantType())
            {
                // 1. Validação básica de usuário e senha
                var user = await _userManager.FindByEmailAsync(request.Username);
                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    return Forbid();
                }

                // Captura o parâmetro "code" enviado no corpo da requisição x-www-form-urlencoded
                var code = request.GetParameter("code")?.Value?.ToString();

                // ------------------------------------------------------------------
                // PASSO 1: O usuário enviou a senha correta, mas ainda não informou o código
                // ------------------------------------------------------------------
                if (string.IsNullOrEmpty(code))
                {
                    // Gera um código seguro de 2 fatores atrelado ao Identity
                    //var mfaCode = await _userManager.GenerateUserTokenAsync(user, "Default", "2FAEmailAuth");
                    var mfaCode = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                    // Renderiza o template HTML criado
                    var htmlBody = EmailTemplates.GetTwoFactorTemplate(user.UserName, mfaCode);

                    // Dispara o e-mail de forma assíncrona
                    await _emailService.SendEmailAsync(user.Email, "Seu código de acesso RentaInvest", htmlBody);

                    // Retorna um BadRequest padronizado informando ao Frontend que o 2FA é necessário
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = "mfa_required",
                        ErrorDescription = "Autenticação de dois fatores obrigatória. O código foi enviado para o seu e-mail.",
                    });
                }

                // ------------------------------------------------------------------
                // PASSO 2: O usuário enviou a senha E o código do e-mail
                // ------------------------------------------------------------------
                //var isValidCode = await _userManager.VerifyUserTokenAsync(user, "Default", "2FAEmailAuth", code);
                var isValidCode = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, code);
                if (!isValidCode)
                {
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = OpenIddictConstants.Errors.InvalidGrant,
                        ErrorDescription = "Código de verificação de e-mail inválido ou expirado."
                    });
                }

                // --- SEU BLOCO DE CONFIGURAÇÃO DE CLAIMS E TOKEN (Mantido idêntico) ---
                var roles = await _userManager.GetRolesAsync(user);
                var principal = await _signInManager.CreateUserPrincipalAsync(user);
                var identity = (ClaimsIdentity)principal.Identity;

                identity.RemoveClaim(identity.FindFirst(OpenIddictConstants.Claims.Subject));
                identity.RemoveClaim(identity.FindFirst(OpenIddictConstants.Claims.Name));

                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName));

                identity.SetAccessTokenLifetime(TimeSpan.FromHours(5));
                principal.SetAccessTokenLifetime(TimeSpan.FromHours(5));

                principal.SetDestinations(static claim => claim.Type switch
                {
                    OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Role
                        => new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken },

                    _ => new[] { OpenIddictConstants.Destinations.AccessToken }
                });

                principal.SetScopes(new[]
                {
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Roles,
                });

                principal.SetResources("selfInvestApp-idd");

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant_type is not supported."
            });
        }


        // ---  Solicitador do Código por E-mail ---
        [HttpPost("request-code")]
        public async Task<IActionResult> RequestCode([FromBody] RequestCodeDto model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email.Trim());
                if (user == null)
                {
                    // Retornamos OK mesmo se não achar para evitar enumeração de e-mails por hackers
                    return Ok(new { success = true, message = "Se o e-mail estiver cadastrado, um código foi enviado." });
                }

                // Gera um token seguro de uso único atrelado ao provedor padrão do Identity
                var mfaCode = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                // Renderiza o template HTML criado
                var htmlBody = EmailTemplates.GetTwoFactorTemplate(user.UserName, mfaCode);

                // Dispara o e-mail de forma assíncrona
                await _emailService.SendEmailAsync(user.Email, "Seu código de acesso RentaInvest", htmlBody);

                // Retornando o código no JSON temporariamente apenas para você testar no Postman/Swagger:
                return Ok(new { success = true, message = "Código enviado com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, errors = ex.Message });
            }
        }
    }
}
