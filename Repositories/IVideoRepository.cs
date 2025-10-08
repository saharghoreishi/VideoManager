using VideoManager.Api.Common;
using VideoManager.Api.Models;

namespace VideoManager.Api.Repositories
{
    public interface IVideoRepository
    {
        Task<Video?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<PagedResult<Video>> QueryAsync(string? search, string? sortBy, string? sortDir, int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(Video video, CancellationToken ct = default);
        Task UpdateAsync(Video video, CancellationToken ct = default);
        Task DeleteAsync(Video video, CancellationToken ct = default);
    }
}
