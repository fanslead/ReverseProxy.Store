using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReverseProxy.WebApi.Contracts.V1.Auth;
using ReverseProxy.WebApi.Data;

namespace ReverseProxy.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("antiforgery")]
    [ProducesResponseType<AntiforgeryTokenResponse>(StatusCodes.Status200OK)]
    public ActionResult<AntiforgeryTokenResponse> GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new AntiforgeryTokenResponse(
            tokens.RequestToken ?? throw new InvalidOperationException("An antiforgery request token was not generated."),
            tokens.HeaderName ?? "X-CSRF-TOKEN"));
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Account temporarily locked",
                Detail = "Too many failed sign-in attempts. Try again later."
            });
        }

        if (!result.Succeeded)
        {
            return UnauthorizedProblem();
        }

        return Ok(await CreateCurrentUserResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? UnauthorizedProblem() : Ok(await CreateCurrentUserResponseAsync(user));
    }

    private async Task<CurrentUserResponse> CreateCurrentUserResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.DisplayName,
            roles.Order(StringComparer.Ordinal).ToArray());
    }

    private ObjectResult UnauthorizedProblem()
    {
        return StatusCode(StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication failed",
            Detail = "The supplied credentials are invalid."
        });
    }
}