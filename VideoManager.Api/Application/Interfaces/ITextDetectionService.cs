namespace VideoManager.Api.Application.Interfaces
{
    public interface ITextDetectionService
    {
        Task<bool> ContainsTextAsync(string videoPath, string targetText, double sampleRateFps = 1.0, CancellationToken cancellationToken = default);
    }
}