namespace VideoManager.Api.Infrastructure.Data
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using VideoManager.Api.Domain.Auth;
    using VideoManager.Api.Domain.Models;

    public class AppDbContext(DbContextOptions<AppDbContext> options)
            : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)  
    {
        public DbSet<Video> Videos => Set<Video>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>().HasIndex(v => v.Title);
            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.UserId).IsRequired();
                b.Property(x => x.Token).IsRequired();
                b.Property(x => x.JwtId).IsRequired();
                b.Property(x => x.ExpiresAt).IsRequired();
                b.HasIndex(x => x.Token).IsUnique();
            });

            base.OnModelCreating(modelBuilder);
        }
    }

}
