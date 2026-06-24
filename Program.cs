using AuthServer.Config;
using AuthServer.Infra;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager Configuration = builder.Configuration;


// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//log cinfig
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(LogEventLevel.Debug)
    .WriteTo.File(Configuration["LoggingFilePath"], LogEventLevel.Debug, rollingInterval: RollingInterval.Day));

// DbConfig
string connectionStr = Configuration["ConnectionStrings:DefaultConnection"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseOpenIddict<Application, Authorization, Scope, Token, string>();
    
    options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr), op => op.EnableStringComparisonTranslations());

    // Ative recursos sensveis (apenas para desenvolvimento)
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
});

// Injeção de dependência para o serviço de email
builder.Services.AddTransient<IEmailService, EmailService>();

// Configurar IdentityServer

// Configure o Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

bool isDev = builder.Environment.IsDevelopment();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
});

if (builder.Environment.IsDevelopment())
{
    // Habilita logs detalhados de tokens no console em ambiente de desenvolvimento
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>()
               .ReplaceDefaultEntities<Application, Authorization, Scope, Token, string>();
    })
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(Configuration["OpenIddict:Server:Issuer"]));

        // Força o OpenIddict a emitir um JWT normal (sem JWE)
        options.DisableAccessTokenEncryption();

        options.SetAuthorizationEndpointUris(Configuration["OpenIddict:Server:AuthorizationEndpoint"])
               .SetTokenEndpointUris(Configuration["OpenIddict:Server:TokenEndpoint"])
               .SetIntrospectionEndpointUris(Configuration["OpenIddict:Server:IntrospectionEndpoint"])
               .SetRevocationEndpointUris(Configuration["OpenIddict:Server:RevocationEndpoint"])
               .SetUserInfoEndpointUris(Configuration["OpenIddict:Server:UserInfoEndpoint"]);

        options.SetJsonWebKeySetEndpointUris(Configuration["OpenIddict:Server:JwksEndpoint"]);

        options.AddDevelopmentEncryptionCertificate()
               //.AddDevelopmentSigningCertificate()
               .Configure(a => a.Claims.Add(ClaimTypes.Role));

        options.AddEphemeralSigningKey()
       .AddEphemeralEncryptionKey();

        if (builder.Environment.IsDevelopment())
        {
            options.UseAspNetCore()
                .EnableTokenEndpointPassthrough()
                .EnableAuthorizationEndpointPassthrough()
                .DisableTransportSecurityRequirement();
        }
        else
        {
            options.UseAspNetCore()
             .EnableTokenEndpointPassthrough()
             .EnableAuthorizationEndpointPassthrough();
        }

        // Cria uma chave assimétrica RSA em memória para o ciclo de vida da aplicação
        var asymmetricRsaKey = new RsaSecurityKey(RSA.Create(2048))
        {
            KeyId = "rentainvest-asymmetric-signing-key"
        };

        options.AddSigningKey(asymmetricRsaKey);


        options.AllowClientCredentialsFlow();
        options.AllowPasswordFlow();
        options.AllowCustomFlow("email_code");

        //options.SetAccessTokenLifetime(TimeSpan.FromHours(48));
        //options.SetIdentityTokenLifetime(TimeSpan.FromHours(48));
        //options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        options.AllowRefreshTokenFlow();

        options.RegisterScopes(OpenIddictConstants.Permissions.Scopes.Email,
                       OpenIddictConstants.Permissions.Scopes.Profile,
                       OpenIddictConstants.Permissions.Scopes.Roles);

        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ApplyTokenResponseContext>(builder =>
        builder.UseInlineHandler(context =>
        {
            var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
            if (context == null || httpContext == null) return default;

            if (string.IsNullOrEmpty(context.Response.Error) && !string.IsNullOrEmpty(context.Response.AccessToken))
            {
                // 1. Recupera o ClientId da requisição que gerou o token
                var clientId = context.Request?.ClientId;

                // 2. Define dinamicamente o nome do cookie
                string cookieName = clientId switch
                {
                    "selfInvestApp-idd" => "selfinvest_token",
                    "internationalRentainvestApp-idd" => "intl_rentainvest_token",
                    "rentainvestApp-idd" => "rentainvest_token",
                    _ => "app_token" // Fallback de segurança caso venha nulo ou desconhecido
                };

                // 3. Adiciona o cookie no response com o nome correto
                httpContext.Response.Cookies.Append(cookieName, context.Response.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !isDev,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(48)
                });

                // 4. Remove o token do corpo da resposta JSON
                context.Response.AccessToken = null;
            }
            return default;
        }));
        //options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ApplyTokenResponseContext>(builder =>
        //builder.UseInlineHandler(context =>
        //{
        //    var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        //    if (context == null || httpContext == null) return default;

        //    if (string.IsNullOrEmpty(context.Response.Error) && !string.IsNullOrEmpty(context.Response.AccessToken))
        //    {
        //        httpContext.Response.Cookies.Append("rentainvest_token", context.Response.AccessToken, new CookieOptions
        //        {
        //            HttpOnly = true,
        //            Secure = !isDev,
        //            SameSite = SameSiteMode.Lax,
        //            Expires = DateTimeOffset.UtcNow.AddHours(48)
        //        });

        //        context.Response.AccessToken = null;
        //    }
        //    return default;
        //}));
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
        options.SetClientAssertionLifetime(TimeSpan.FromHours(48));
    });

//builder.Services.AddOpenIddict()
//    .AddCore(options =>
//    {
//        options.UseEntityFrameworkCore()
//               .UseDbContext<ApplicationDbContext>()
//               .ReplaceDefaultEntities<Application, Authorization, Scope, Token, string>(); 
//    })
//    .AddServer(options =>
//    {
//        options.SetIssuer(new Uri(Configuration["OpenIddict:Server:Issuer"]));

//        //options.SetIssuer(Configuration["OpenIddict:Server:Issuer"]);

//        options.SetAuthorizationEndpointUris(Configuration["OpenIddict:Server:AuthorizationEndpoint"])
//               .SetTokenEndpointUris(Configuration["OpenIddict:Server:TokenEndpoint"])
//               .SetIntrospectionEndpointUris(Configuration["OpenIddict:Server:IntrospectionEndpoint"])
//               .SetRevocationEndpointUris(Configuration["OpenIddict:Server:RevocationEndpoint"])
//               .SetUserInfoEndpointUris(Configuration["OpenIddict:Server:UserInfoEndpoint"]);

//        options.SetJsonWebKeySetEndpointUris(Configuration["OpenIddict:Server:JwksEndpoint"]);

//        // Emitir tokens JWT assinados
//        options.AddEphemeralSigningKey(); // Use um certificado para produo
//        options.AddDevelopmentEncryptionCertificate() // Add a development encryption certificate for encryption
//               .AddDevelopmentSigningCertificate()
//                .Configure(a => a.Claims.Add(ClaimTypes.Role)); // Add a development signing certificate for signing


//        if (builder.Environment.IsDevelopment())
//        {
//            options.UseAspNetCore()
//                .EnableTokenEndpointPassthrough()
//                .EnableAuthorizationEndpointPassthrough()
//                .DisableTransportSecurityRequirement();

//        }
//        else
//        {
//            options.UseAspNetCore()
//             .EnableTokenEndpointPassthrough()
//             .EnableAuthorizationEndpointPassthrough();
//        }

//        var secretKey = Configuration["Secrets:Admin_secret"]; // Chave secreta armazenada no arquivo de configura��o
//        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

//        options.AddSigningKey(signingKey); // Adicionando a chave de assinatura


//        options.AllowClientCredentialsFlow(); // Suporta autenticao de clientes
//        options.AllowPasswordFlow(); // Suporta autentica por senha
//        options.AllowCustomFlow("email_code");

//        options.AddEphemeralSigningKey();

//        options.SetAccessTokenLifetime(TimeSpan.FromHours(48));
//        options.SetIdentityTokenLifetime(TimeSpan.FromHours(48));
//        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

//        options.AllowRefreshTokenFlow();

//        options.RegisterScopes(OpenIddictConstants.Permissions.Scopes.Email,
//                       OpenIddictConstants.Permissions.Scopes.Profile,
//                       OpenIddictConstants.Permissions.Scopes.Roles);

//        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ApplyTokenResponseContext>(builder =>
//        builder.UseInlineHandler(context =>
//        {
//            var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;

//            if (context == null)
//            {
//                return default;
//            }

//            // Verifica se a resposta foi gerada com sucesso e se contém o AccessToken
//            if (string.IsNullOrEmpty(context.Response.Error) && !string.IsNullOrEmpty(context.Response.AccessToken))
//            {
//                httpContext.Response.Cookies.Append("rentainvest_token", context.Response.AccessToken, new CookieOptions
//                {
//                    HttpOnly = true,   // Protege contra XSS (VueJS não lê)
//                    Secure = !isDev,     // Apenas HTTPS
//                    SameSite = SameSiteMode.Lax, // Protege contra CSRF
//                    Expires = DateTimeOffset.UtcNow.AddHours(48) // Alinhado com as 48h do token
//                });


//                // 3. SEGURANÇA EXTRA: Apaga o access_token do corpo do JSON
//                // Assim, mesmo se sua app sofrer um ataque XSS, o hacker não consegue ler o token pelo payload do Axios
//                context.Response.AccessToken = null;
//            }
//            return default;
//        }));

//    })
//    .AddValidation(options =>
//    {
//        options.UseLocalServer();
//        options.UseAspNetCore();
//        options.SetClientAssertionLifetime(TimeSpan.FromHours(48));
//    });


// PolicyCors
var withOrigins = Configuration.GetSection("withOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueJsApp",
        builder =>
        {
            builder.WithOrigins(withOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithMethods("GET", "POST")
                                .AllowCredentials();
        });
});

builder.Services.Configure<IISOptions>(o =>
{
    o.ForwardClientCertificate = false;
});

// Configurar autentica  o JWT

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = Configuration["Secrets:Authority"];
    //options.Audience = Configuration["Secrets:ValidAudience"];

    options.TokenValidationParameters = new TokenValidationParameters
    {
       
        //  CORREÇÃO: Aceitar qualquer uma das suas 4 aplicações cadastradas
        ValidAudiences = new[]
        {
            "rentainvestApp-idd",
            "internationalRentainvestApp-idd",
            "selfInvestApp-idd",
            "clientPainel_app"
        }
    };

    options.RequireHttpsMetadata = builder.Environment.IsProduction();
});

//builder.Services.AddAuthorizationBuilder()
//    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
//    .AddPolicy("UserOnly", policy => policy.RequireRole("Client"));

if (builder.Environment.IsProduction())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"/var/www/keys"))
    .SetApplicationName("RentaInvest");
}
else
{
    builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Users\gusta\Documents\Desenvolvimento\rentainvest\keys"))
.SetApplicationName("RentaInvest");
}

builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

app.UsePathBase("/identity");

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownProxies.Clear();  // Remove a trava de IP estrita do .NET
forwardedOptions.KnownNetworks.Clear(); // Remove a trava de subrede do .NET

app.UseForwardedHeaders(forwardedOptions);

// Seed roles e usu rios
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var seeder = services.GetRequiredService<DataSeeder>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    InicializeDb.Initialize(dbContext);

    await seeder.SeedOpenIddictAsync();
    //await seeder.Initialize();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}



app.UseCors("AllowVueJsApp");

app.UseRouting();



// Use autentica��o e autoriza��o
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();

