namespace VideoManager.Models
{
    public class Video
    {
        public int Id{ get; set; }

        public required string Title { get; set; }

        public  string? Url { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
