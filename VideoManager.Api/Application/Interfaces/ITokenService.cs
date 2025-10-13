using VideoManager.Api.Domain.Auth;
namespace VideoManager.Api.Application.Interfaces
{
    public record TokenPair(string AccessToken, string RefreshToken);

    public interface ITokenService
    {
        Task<TokenPair> CreateAsync(ApplicationUser user);
        Task<TokenPair?> RefreshAsync(string refreshToken, string accessToken);
        Task RevokeAsync(string refreshToken);
    }

}