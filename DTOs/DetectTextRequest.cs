namespace VideoManager.Api.DTOs
{
    public class DetectTextRequest
    {
        public string? VideoPath { get; set; }
        public string? TargetText { get; set; }
        public double? SampleRateFps { get; set; }
    }
}
