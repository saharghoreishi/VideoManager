using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VideoManager.Api.Application.Interfaces;
using VideoManager.Api.Domain.Auth;
namespace VideoManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm, ITokenService tokens) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _um = um;
        private readonly SignInManager<ApplicationUser> _sm = sm;
        private readonly ITokenService _tokens = tokens;

        public record RegisterDto(string Email, string Password);
        public record LoginDto(string Email, string Password, string? TwoFactorCode);
        public record RefreshDto(string AccessToken, string RefreshToken);

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
            var result = await _um.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);
            var pair = await _tokens.CreateAsync(user);
            return Ok(pair);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _um.FindByEmailAsync(dto.Email);
            if (user is null) return Unauthorized();

            if (await _um.GetTwoFactorEnabledAsync(user))
            {
                if (string.IsNullOrWhiteSpace(dto.TwoFactorCode)) return BadRequest("2FA required.");
                var valid2fa = await _um.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, dto.TwoFactorCode);
                if (!valid2fa) return Unauthorized("Invalid 2FA code.");
            }
            else
            {
                var check = await _sm.CheckPasswordSignInAsync(user, dto.Password, true);
                if (!check.Succeeded) return Unauthorized();
            }

            var pair = await _tokens.CreateAsync(user);
            return Ok(pair);
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh(RefreshDto dto)
        {
            var pair = await _tokens.RefreshAsync(dto.RefreshToken, dto.AccessToken);
            return pair is null ? Unauthorized() : Ok(pair);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] string refreshToken)
        {
            await _tokens.RevokeAsync(refreshToken);
            return Ok();
        }
    }
}
