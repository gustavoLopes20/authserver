using AuthServer.Config;
using AuthServer.Infra;
using AuthServer.Models;
using AuthServer.Services;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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


builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
});

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

        //options.SetIssuer(Configuration["OpenIddict:Server:Issuer"]);

        options.SetAuthorizationEndpointUris(Configuration["OpenIddict:Server:AuthorizationEndpoint"])
               .SetTokenEndpointUris(Configuration["OpenIddict:Server:TokenEndpoint"])
               .SetIntrospectionEndpointUris(Configuration["OpenIddict:Server:IntrospectionEndpoint"])
               .SetRevocationEndpointUris(Configuration["OpenIddict:Server:RevocationEndpoint"])
               .SetUserInfoEndpointUris(Configuration["OpenIddict:Server:UserInfoEndpoint"]);

        options.SetJsonWebKeySetEndpointUris(Configuration["OpenIddict:Server:JwksEndpoint"]);

        // Emitir tokens JWT assinados
        options.AddEphemeralSigningKey(); // Use um certificado para produo
        options.AddDevelopmentEncryptionCertificate() // Add a development encryption certificate for encryption
               .AddDevelopmentSigningCertificate()
                .Configure(a => a.Claims.Add(ClaimTypes.Role)); // Add a development signing certificate for signing


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

        var secretKey = Configuration["Secrets:Admin_secret"]; // Chave secreta armazenada no arquivo de configura��o
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        options.AddSigningKey(signingKey); // Adicionando a chave de assinatura


        options.AllowClientCredentialsFlow(); // Suporta autentica��o de clientes
        options.AllowPasswordFlow(); // Suporta autentica por senha
        options.AllowCustomFlow("email_code");

        options.AddEphemeralSigningKey();

        options.SetAccessTokenLifetime(TimeSpan.FromHours(5));
        options.SetIdentityTokenLifetime(TimeSpan.FromHours(5));
        options.SetRefreshTokenLifetime(TimeSpan.FromHours(5));

        options.AllowRefreshTokenFlow();

        options.RegisterScopes(OpenIddictConstants.Permissions.Scopes.Email,
                       OpenIddictConstants.Permissions.Scopes.Profile,
                       OpenIddictConstants.Permissions.Scopes.Roles);


    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
        options.SetClientAssertionLifetime(TimeSpan.FromHours(5));
    });


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
                .AllowCredentials()
                .WithMethods("GET", "POST");
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
    options.Audience = Configuration["Secrets:ValidAudience"];

    options.RequireHttpsMetadata = builder.Environment.IsProduction();
});

//builder.Services.AddAuthorizationBuilder()
//    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
//    .AddPolicy("UserOnly", policy => policy.RequireRole("Client"));

if (builder.Environment.IsProduction())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"/var/www/authServer2/keys"))
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


// Seed roles e usu rios
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var seeder = services.GetRequiredService<DataSeeder>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    InicializeDb.Initialize(dbContext);

    await seeder.SeedOpenIddictAsync();
    await seeder.Initialize();
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


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("AllowVueJsApp");

app.UseRouting();



// Use autentica��o e autoriza��o
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();

