// =====================================
// Unit Tests — AuthControllerTests.cs
// =====================================
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VideoManager.Api.Application.Interfaces;
using VideoManager.Api.Controllers;
using VideoManager.Api.Domain.Auth;
using Xunit;


namespace VideoManager.Test;


public class AuthControllerTests
{
    private static UserManager<ApplicationUser> MakeUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new UserManager<ApplicationUser>(store.Object, null!, new PasswordHasher<ApplicationUser>(), null!, null!, null!, null!, null!, null!);
    }


    private static SignInManager<ApplicationUser> MakeSignInManager(UserManager<ApplicationUser> um)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new SignInManager<ApplicationUser>(um, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }


    [Fact]
    public async Task Register_ReturnsOk_WithTokenPair_WhenCreateSucceeds()
    {
        // arrange
        var um = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
        .ReturnsAsync(IdentityResult.Success);


        var sm = new Mock<SignInManager<ApplicationUser>>(um.Object,
        Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
        Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null!, null!, null!, null!);


        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.CreateAsync(It.IsAny<ApplicationUser>()))
        .ReturnsAsync(new TokenPair("access", "refresh"));


        var sut = new AuthController(um.Object, sm.Object, tokens.Object);


        // act
        var result = await sut.Register(new AuthController.RegisterDto("u@e.com", "Passw0rd!"));


        // assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
    }
    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        var user = new ApplicationUser { Email = "u@e.com", UserName = "u@e.com" };


        var um = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByEmailAsync("u@e.com")).ReturnsAsync(user);
        um.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);


        var sm = new Mock<SignInManager<ApplicationUser>>(um.Object,
        Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
        Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null!, null!, null!, null!);
        sm.Setup(x => x.CheckPasswordSignInAsync(user, "wrong", true))
        .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);


        var tokens = new Mock<ITokenService>();
        var sut = new AuthController(um.Object, sm.Object, tokens.Object);


        var result = await sut.Login(new AuthController.LoginDto("u@e.com", "wrong", null!));
        result.Should().BeOfType<UnauthorizedResult>();
    }


    [Fact]
    public async Task Refresh_ReturnsOk_WhenTokenServiceReturnsPair()
    {
        var um = MakeUserManager();
        var sm = MakeSignInManager(um);
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.RefreshAsync("r", "a"))
        .ReturnsAsync(new TokenPair("a2", "r2"));


        var sut = new AuthController(um, sm, tokens.Object);
        var result = await sut.Refresh(new AuthController.RefreshDto("a", "r"));
        result.Should().BeOfType<OkObjectResult>();
    }


    [Fact]
    public async Task Logout_CallsRevoke_AndReturnsOk()
    {
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.RevokeAsync("r")).Returns(Task.CompletedTask).Verifiable();
        var sut = new AuthController(MakeUserManager(), MakeSignInManager(MakeUserManager()), tokens.Object);


        var result = await sut.Logout("r");
        tokens.Verify();
        result.Should().BeOfType<OkResult>();
    }
}