namespace VideoManager.Api.Auth
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = default!;
        public string? JwtId { get; set; }  // ← nullable
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; }
        public bool IsUsed { get; set; }
        public string UserId { get; set; } = default!;
    }
}
