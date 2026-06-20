using AuthServer.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace AuthServer.Infra
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
    {
        public DbSet<Application> Applications { get; set; }
        public DbSet<Authorization> Authorizations { get; set; }
        public DbSet<Token> Tokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.UseOpenIddict<Application, Authorization, Scope, Token, string>();

            builder.Entity<Application>()
                .Property(a => a.CustomPermissions)
                .HasConversion(new StringListConverter())
                .HasColumnName("Permissions");
        }
    }
}
