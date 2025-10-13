using System.ComponentModel.DataAnnotations;

namespace VideoManager.Api.Domain.Models
{
    public class Video
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, Url, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
