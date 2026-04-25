using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
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

    // ── Step 1: Login ─────────────────────────────────────────────────────────
    // Returns list of companies the user can access + a short-lived temp token.
    // Frontend uses this list to show the company selector screen.

    public record LoginRequest(string Username, string Password);
    public record CompanyInfo(string Code, string Name, string ShortName, string? JobLetter, string? LogoUrl);
    public record LoginResponse(string TempToken, string Username, IEnumerable<CompanyInfo> Companies);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Company)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Active);

        if (user == null || !_password.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        // Build company list — user's explicitly assigned companies
        var companies = user.UserCompanies
            .Where(uc => uc.Company != null && uc.Company.Active)
            .Select(uc => new CompanyInfo(
                uc.Company!.CompanyCode,
                uc.Company.Name,
                uc.Company.ShortName,
                uc.Company.JobLetter,
                uc.Company.LogoUrl))
            .ToList();

        // Admins and Analytics roles also get Global Analytics access
        if (roles.Any(r => r is "Administrator" or "Analytics"))
        {
            companies.Add(new CompanyInfo("GLOBAL", "Global Analytics", "GLOBAL", null, null));
        }

        var tempToken = _jwt.GenerateTempToken(user);

        user.LastLogin = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new LoginResponse(tempToken, user.Username, companies));
    }

    // ── Step 2: Select Company ────────────────────────────────────────────────
    // Validates the temp token + company access, then issues a full JWT.

    public record SelectCompanyRequest(string TempToken, string CompanyCode);
    public record AuthResponse(string Token, string Username, string CompanyCode, IEnumerable<string> Roles);

    [HttpPost("select-company")]
    [AllowAnonymous]
    public async Task<IActionResult> SelectCompany([FromBody] SelectCompanyRequest request)
    {
        var userId = _jwt.ValidateTempToken(request.TempToken);
        if (userId == null)
            return Unauthorized(new { message = "Session expired. Please log in again." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserCompanies)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Active);

        if (user == null)
            return Unauthorized(new { message = "User not found." });

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        // GLOBAL is a virtual company — only admins/analytics get it
        if (request.CompanyCode == "GLOBAL")
        {
            if (!roles.Any(r => r is "Administrator" or "Analytics"))
                return Forbid();
        }
        else
        {
            // Verify user has explicit access to this company
            var hasAccess = user.UserCompanies.Any(uc => uc.CompanyCode == request.CompanyCode);
            if (!hasAccess)
                return Forbid();
        }

        var token = _jwt.GenerateToken(user, request.CompanyCode, roles);

        return Ok(new AuthResponse(token, user.Username, request.CompanyCode, roles));
    }

    // ── Refresh (keeps current company) ──────────────────────────────────────

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var companyCode = User.FindFirst("company_code")?.Value ?? string.Empty;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Active);

        if (user == null) return Unauthorized();

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var token = _jwt.GenerateToken(user, companyCode, roles);

        return Ok(new AuthResponse(token, user.Username, companyCode, roles));
    }
}
