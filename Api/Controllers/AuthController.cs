using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IJwtService _jwt;
    private readonly IPasswordService _password;

    public AuthController(IDbContextFactory<AppDbContext> dbFactory, IJwtService jwt, IPasswordService password)
    {
        _dbFactory = dbFactory;
        _jwt = jwt;
        _password = password;
    }

    public record LoginRequest(string Username, string Password, string CompanyCode);
    public record LoginResponse(string Token, string Username, string CompanyCode, IEnumerable<string> Roles);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                u.Username == request.Username &&
                u.CompanyCode == request.CompanyCode &&
                u.Active);

        if (user == null || !_password.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username, password, or company code." });

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var token = _jwt.GenerateToken(user, roles);

        user.LastLogin = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new LoginResponse(token, user.Username, user.CompanyCode, roles));
    }
}
