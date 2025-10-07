
namespace VideoManager.Data
{
    using Microsoft.EntityFrameworkCore;
    using VideoManager.Models;

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Video> Videos => Set<Video>();
    }

}
