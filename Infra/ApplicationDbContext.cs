using AuthServer.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace AuthServer.Infra;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Application> Applications { get; set; }
    public DbSet<Authorization> Authorizations { get; set; }
    public DbSet<Token> Tokens { get; set; }

    // 1. Adicionado o DbSet para a nova tabela de vínculo
    public DbSet<UserApplication> UserApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict<Application, Authorization, Scope, Token, string>();

        builder.Entity<Application>()
            .Property(a => a.CustomPermissions)
            .HasConversion(new StringListConverter())
            .HasColumnName("Permissions");

        // 2. Configuração da chave composta e relacionamento do controle multi-app
        builder.Entity<UserApplication>()
            .HasKey(ua => new { ua.UserId, ua.ClientId });

        builder.Entity<UserApplication>()
            .HasOne(ua => ua.User)
            .WithMany(u => u.AllowedApplications)
            .HasForeignKey(ua => ua.UserId);
    }
}
