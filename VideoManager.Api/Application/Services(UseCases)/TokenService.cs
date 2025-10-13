using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VideoManager.Api.Application.Interfaces;
using VideoManager.Api.Domain.Auth;
using VideoManager.Api.Domain.Models;
using VideoManager.Api.Infrastructure.Data;

namespace VideoManager.Api.Application.Services
{
    public class TokenService(AppDbContext db, UserManager<ApplicationUser> um, IConfiguration cfg) : ITokenService
    {
        private readonly AppDbContext _db = db;
        private readonly UserManager<ApplicationUser> _um = um;
        private readonly IConfiguration _cfg = cfg;

        public async Task<TokenPair> CreateAsync(ApplicationUser user)
        {
            var jwt = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, jti)
            };
            var roles = await _um.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var access = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(int.Parse(jwt["AccessTokenMinutes"]!)),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(access);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            _db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(), 
                UserId = user.Id,
                Token = refreshToken,     
                JwtId = jti,
                ExpiresAt = now.AddDays(int.Parse(jwt["RefreshTokenDays"]!)),
                IsUsed = false,
                IsRevoked = false
            });
            await _db.SaveChangesAsync();

            return new(accessToken, refreshToken);
        }

        public async Task<TokenPair?> RefreshAsync(string refreshToken, string accessToken)
        {
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
            if (existing is null || existing.IsUsed || existing.IsRevoked || existing.ExpiresAt <= DateTime.UtcNow)
                return null;

            var jwt = _cfg.GetSection("Jwt");
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
            }, out var validated);

            if (validated is not JwtSecurityToken j || !j.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                return null;

            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (string.IsNullOrWhiteSpace(jti) || !string.Equals(jti, existing.JwtId, StringComparison.Ordinal))
                return null;

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await _um.FindByIdAsync(userId);
            if (user is null) return null;

            existing.IsUsed = true;
            await _db.SaveChangesAsync();

            var pair = await CreateAsync(user);


            var others = await _db.RefreshTokens
                .Where(r => r.UserId == user.Id && !r.IsUsed && !r.IsRevoked && r.Token != pair.RefreshToken)
                .ToListAsync();
            foreach (var o in others) o.IsRevoked = true;
            await _db.SaveChangesAsync();

            return pair;
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
