using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VideoManager.Api.Auth;
using VideoManager.Api.Data;

namespace VideoManager.Api.Services
{
    public class TokenService(AppDbContext db, UserManager<ApplicationUser> um, IConfiguration cfg) : ITokenService
    {
        private readonly AppDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = um;
        private readonly IConfiguration _cfg = cfg;

        public async Task<TokenPair> CreateAsync(ApplicationUser user)
        {
            var jwtSection = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
        };

            var now = DateTime.UtcNow;
            var access = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(int.Parse(jwtSection["AccessTokenMinutes"]!)),
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(access);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            _db.RefreshTokens.Add(new()
            {
                Token = refreshToken,
                UserId = user.Id,
                JwtId = access.Id,
                ExpiresAt = now.AddDays(int.Parse(jwtSection["RefreshTokenDays"]!))
            });

            await _db.SaveChangesAsync();
            return new(accessToken, refreshToken);
        }

        public async Task<TokenPair?> RefreshAsync(string refreshToken, string accessToken)
        {
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
            if (existing is null || existing.IsUsed || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
                return null;

            // Reuse detection
            existing.IsUsed = true;
            await _db.SaveChangesAsync();

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            var userId = jwt.Subject;
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return null;

            // Rotation: صدور جفت توکن جدید
            var newPair = await CreateAsync(user);

            // Revoke تمام RefreshToken‌های قدیمی این کاربر که هنوز استفاده نشده‌اند (سفت‌گیرانه)
            var others = await _db.RefreshTokens
                .Where(r => r.UserId == user.Id && !r.IsUsed && !r.IsRevoked && r.Token != newPair.RefreshToken)
                .ToListAsync();

            foreach (var r in others) r.IsRevoked = true;
            await _db.SaveChangesAsync();

            return newPair;
        }

        public async Task RevokeAsync(string refreshToken)
        {
            var t = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
            if (t is null) return;
            t.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }
}
