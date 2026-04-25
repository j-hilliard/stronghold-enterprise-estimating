using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    private (SymmetricSecurityKey key, string issuer, string audience, int expiryMinutes) GetConfig()
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var issuer = _config["Jwt:Issuer"] ?? "stronghold-estimating";
        var audience = _config["Jwt:Audience"] ?? "stronghold-estimating";
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "480");
        return (key, issuer, audience, expiryMinutes);
    }

    // Original method — uses user.CompanyCode (backward compat)
    public string GenerateToken(User user, IEnumerable<string> roles)
        => GenerateToken(user, user.CompanyCode, roles);

    // Explicit company code — used in select-company flow
    public string GenerateToken(User user, string companyCode, IEnumerable<string> roles)
    {
        var (key, issuer, audience, expiryMinutes) = GetConfig();
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("company_code", companyCode),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Short-lived token for the company selection step (5 min, no company claim)
    public string GenerateTempToken(User user)
    {
        var (key, issuer, audience, _) = GetConfig();
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("auth_step", "company_select"),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Returns userId from a valid temp token, or null if invalid/expired
    public int? ValidateTempToken(string tempToken)
    {
        try
        {
            var secret = _config["Jwt:Secret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(tempToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var step = principal.FindFirst("auth_step")?.Value;
            if (step != "company_select") return null;

            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var uid) ? uid : null;
        }
        catch
        {
            return null;
        }
    }
}
