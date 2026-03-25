using MediatR;
using Stronghold.EnterpriseEstimating.Api.Authorization;
using Stronghold.EnterpriseEstimating.Shared.Enumerations;

namespace Stronghold.EnterpriseEstimating.Api.Domain;

/// <summary>Returns the username and company code from the current JWT claims.</summary>
public record CurrentUserClaims(string Username, string CompanyCode, List<string> Roles);

[AllowedAuthorizationRole(
    AuthorizationRole.Administrator,
    AuthorizationRole.Estimator,
    AuthorizationRole.Viewer,
    AuthorizationRole.Analytics,
    AuthorizationRole.AuthenticatedUser
)]
public class GetCurrentUser : IRequest<CurrentUserClaims> { }

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUser, CurrentUserClaims>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetCurrentUserHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<CurrentUserClaims> Handle(GetCurrentUser request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("No authenticated user.");

        var username = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? user.FindFirst("username")?.Value
                    ?? string.Empty;

        var companyCode = user.FindFirst("company_code")?.Value ?? string.Empty;

        var roles = user.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        return Task.FromResult(new CurrentUserClaims(username, companyCode, roles));
    }
}
