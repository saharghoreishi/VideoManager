using Microsoft.EntityFrameworkCore;
using VideoManager.Api.Common;
using VideoManager.Api.Data;
using VideoManager.Api.Models;

namespace VideoManager.Api.Repositories
{
    public class VideoRepository(AppDbContext db) : IVideoRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<Video?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _db.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);

        public async Task<PagedResult<Video>> QueryAsync(string? search, string? sortBy, string? sortDir, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Videos.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(v => v.Title.Contains(s, StringComparison.CurrentCultureIgnoreCase));
            }

            // Sorting
            var sb = (sortBy ?? "createdAt").ToLower();
            var sd = (sortDir ?? "desc").ToLower();
            query = (sb, sd) switch
            {
                ("title", "asc") => query.OrderBy(v => v.Title),
                ("title", "desc") => query.OrderByDescending(v => v.Title),
                ("createdat", "asc") => query.OrderBy(v => v.CreatedAt),
                _ => query.OrderByDescending(v => v.CreatedAt) // default
            };

            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return new PagedResult<Video>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task AddAsync(Video video, CancellationToken ct = default)
        {
            _db.Videos.Add(video);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Video video, CancellationToken ct = default)
        {
            _db.Videos.Update(video);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Video video, CancellationToken ct = default)
        {
            _db.Videos.Remove(video);
            await _db.SaveChangesAsync(ct);
        }
    }
}
