namespace VideoManager.Api.Domain.Auth
{
    public sealed class JwtSettings
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Key { get; set; } = null!;
        public int AccessTokenMinutes { get; set; } = 60;
        public int RefreshTokenDays { get; set; } = 7;
    }
}
