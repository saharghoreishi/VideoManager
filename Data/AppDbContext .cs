namespace VideoManager.Api.Data
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using VideoManager.Api.Auth;
    using VideoManager.Api.Models;

    public class AppDbContext(DbContextOptions<AppDbContext> options)
            : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)  
    {
        public DbSet<Video> Videos => Set<Video>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>().HasIndex(v => v.Title);
            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.Property(x => x.Token).IsRequired();
                e.HasIndex(x => x.Token).IsUnique();
                e.Property(x => x.JwtId).IsRequired(false); 
            });
            base.OnModelCreating(modelBuilder);
        }
    }

}
