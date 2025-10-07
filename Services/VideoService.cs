
using Microsoft.EntityFrameworkCore;
using VideoManager.Data;
using VideoManager.Models;

namespace VideoManager.Services
{
    public class VideoService(AppDbContext context) : IVideoService
    {
        public async Task<IEnumerable<Video>> GetAllAsync()
        => await context.Videos.ToListAsync();

        public async Task<Video> CreateAsync(Video video)
        {
            context.Videos.Add(video);
            await context.SaveChangesAsync();
            return video;
        }
    }
}
