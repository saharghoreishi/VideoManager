using VideoManager.Models;

namespace VideoManager.Services
{
    public interface IVideoService
    {
        Task<IEnumerable<Video>> GetAllAsync();
        Task<Video> CreateAsync(Video video);
    }
}
