using VideoManager.Api.Auth;
namespace VideoManager.Api.Services
{
    public record TokenPair(string AccessToken, string RefreshToken);

    public interface ITokenService
    {
        Task<TokenPair> CreateAsync(ApplicationUser user);
        Task<TokenPair?> RefreshAsync(string refreshToken, string accessToken);
        Task RevokeAsync(string refreshToken);
    }

}