using VideoManager.Api.Common;
using VideoManager.Api.DTOs;
using VideoManager.Api.Models;

namespace VideoManager.Api.Services
{
    public interface IVideoService
    {
        Task<PagedResult<Video>> GetAsync(string? search, string? sortBy, string? sortDir, int page, int pageSize, CancellationToken ct = default);
        Task<Video> CreateAsync(CreateVideoRequest request, CancellationToken ct = default);
        Task<Video> UpdateAsync(int id, UpdateVideoRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<Video?> GetByIdAsync(int id, CancellationToken ct = default);
    }
}
