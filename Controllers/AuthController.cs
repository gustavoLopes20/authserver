using AuthServer.Dtos;
using AuthServer.Models;
using AuthServer.Services.Helpers;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [Authorize]
        public async Task<IActionResult> GetUrs([FromQuery] string clientId)
        {
            try
            {
                // 1. Busca apenas o usuário que está fazendo a requisição direto no banco
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

                // Validação de segurança caso o token venha corrompido sem ID
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "Não foi possível extrair o ID do usuário do Token." });
                }


                // Roles que o Admin NÃO deve listar (conforme a sua regra de negócio original)
                string[] excludedRoles = ["Api", "Developer"];

                // 2. Monta a Query base filtrando as Roles e ignorando o próprio Admin logado
                var query = _userManager.Users
                    .Where(a => a.Id != userId && !excludedRoles.Contains(a.Role));

                // 3. NOVO FILTRO: Se um ClientId foi enviado na URL, filtra apenas quem tem acesso a ele
                if (!string.IsNullOrEmpty(clientId))
                {
                    query = query.Where(u => u.AllowedApplications.Any(ua => ua.ClientId == clientId));
                }

                // 4. Executa a query final de forma assíncrona e otimizada no banco de dados
                var filteredUsers = await query.ToListAsync();

                return Ok(filteredUsers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize] 
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return Ok(new
            {
                sucesso = true,
                mensagem = "Logout realizado com sucesso."
            });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            try
            {
                //var adminUser = await _userManager.FindByEmailAsync(model.Email.Trim());

                //if (adminUser != null)
                //    await _userManager.DeleteAsync(adminUser);
                
                var user = new ApplicationUser 
                {
                    NomePessoa = model.UserName.Trim(), 
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(), 
                    Role = model.Role.Trim(), 
                    ExternalId = model.ExternalReferenceId?.Trim(),
                    EmailAlternativo = model.EmailAlternativo?.Trim(),
                    // Mapeia os ClientIds enviados para a nova tabela
                    AllowedApplications = model.AllowedClientIds.Select(clientId => new UserApplication
                    {
                        ClientId = clientId
                    }).ToList()
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
        [Authorize]
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

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("O request do OpenID Connect NAO pode ser recuperado");

            if (request.IsPasswordGrantType())
            {
                // 1. Validação básica de usuário e senha
                var user = await _userManager.FindByEmailAsync(request.Username);
                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    return Forbid();
                }

                // ------------------------------------------------------------------
                // NOVA VALIDAÇÃO: Controle de Acesso por Aplicação
                // ------------------------------------------------------------------
                // Busca a lista de ClientIds permitidos diretamente da tabela de relacionamento
                var userAllowedApps = await _userManager.Users
                    .Where(u => u.Id == user.Id)
                    .SelectMany(u => u.AllowedApplications.Select(a => a.ClientId))
                    .ToListAsync();

                // Se o ClientId atual do request não estiver na lista do usuário, barra o login
                if (!userAllowedApps.Contains(request.ClientId))
                {
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = OpenIddictConstants.Errors.InvalidGrant,
                        ErrorDescription = "Seu usuário não tem permissão para acessar esta aplicação específica."
                    });
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
                    List<string> toEmails = [user.Email, user.EmailAlternativo];

                    await _emailService.SendEmailAsync(toEmails, "Seu código de acesso RentaInvest", htmlBody);

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

                // --- CONFIGURAÇÃO DE CLAIMS E TOKEN  ---
                var roles = await _userManager.GetRolesAsync(user);
                var principal = await _signInManager.CreateUserPrincipalAsync(user);
                var identity = (ClaimsIdentity)principal.Identity;

                // Proteção contra NullReference: Remove apenas se a claim realmente existir
                var subClaim = identity.FindFirst(OpenIddictConstants.Claims.Subject) ?? identity.FindFirst(ClaimTypes.NameIdentifier);
                if (subClaim != null) identity.RemoveClaim(subClaim);

                var nameClaim = identity.FindFirst(OpenIddictConstants.Claims.Name) ?? identity.FindFirst(ClaimTypes.Name);
                if (nameClaim != null) identity.RemoveClaim(nameClaim);

                // Adiciona os formatos padronizados do OpenID Connect
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName));

                identity.SetAccessTokenLifetime(TimeSpan.FromHours(48));
                principal.SetAccessTokenLifetime(TimeSpan.FromHours(48));

                // Propriedades adicionais devolvidas no corpo do JSON do Token
                var properties = new AuthenticationProperties();
                properties.SetParameter("user_id", user.Id);
                properties.SetParameter("user_email", user.Email);
                properties.SetParameter("user_roles", string.Join(",", roles));

                // Unificação dos Destinos: Trata os formatos CURTOS (OpenIddict) e LONGOS (Microsoft) de uma vez só
                principal.SetDestinations(static claim => claim.Type switch
                {
                    OpenIddictConstants.Claims.Name or
                    ClaimTypes.Name or
                    OpenIddictConstants.Claims.Role or
                    ClaimTypes.Role
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

                if (!string.IsNullOrEmpty(request.ClientId))
                {
                    principal.SetResources(request.ClientId);
                }

                return SignIn(principal, properties,OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
                List<string> toEmails = [user.Email, user.EmailAlternativo];
                await _emailService.SendEmailAsync(toEmails, "Seu código de acesso RentaInvest", htmlBody);

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
