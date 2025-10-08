using VideoManager.Api.Common;
using VideoManager.Api.DTOs;
using VideoManager.Api.Models;
using VideoManager.Api.Repositories;

namespace VideoManager.Api.Services
{
    public class VideoService(IVideoRepository repo) : IVideoService
    {
        private readonly IVideoRepository _repo= repo;

        public Task<PagedResult<Video>> GetAsync(string? search, string? sortBy, string? sortDir, int page, int pageSize, CancellationToken ct = default)
            => _repo.QueryAsync(search, sortBy, sortDir, page, pageSize, ct);

        public async Task<Video> CreateAsync(CreateVideoRequest request, CancellationToken ct = default)
        {
            var video = new Video { Title = request.Title.Trim(), Url = request.Url.Trim() };
            await _repo.AddAsync(video, ct);
            return video;
        }

        public async Task<Video> UpdateAsync(int id, UpdateVideoRequest request, CancellationToken ct = default)
        {
            var existing = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"Video {id} not found");
            existing.Title = request.Title.Trim();
            existing.Url = request.Url.Trim();
            await _repo.UpdateAsync(existing, ct);
            return existing;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"Video {id} not found");
            await _repo.DeleteAsync(existing, ct);
        }

        public Task<Video?> GetByIdAsync(int id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);
    }
}
