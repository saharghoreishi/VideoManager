using System.ComponentModel.DataAnnotations;

namespace VideoManager.DTOs
{
    public class CreateVideoRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, Url, MaxLength(500)]
        public string Url { get; set; } = string.Empty;
    }
}
