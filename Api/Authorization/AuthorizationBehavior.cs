using MediatR;
using Stronghold.EnterpriseEstimating.Shared.Enumerations;

namespace Stronghold.EnterpriseEstimating.Api.Authorization;

/// <summary>
/// MediatR pipeline behavior that enforces role-based authorization using JWT claims.
/// Roles are read directly from the ClaimsPrincipal — no database lookup required.
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;

    public AuthorizationBehavior(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment env
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var attribute =
            Attribute.GetCustomAttribute(request.GetType(), typeof(AllowedAuthorizationRole))
            as AllowedAuthorizationRole;

        // If no attribute is present, allow through (controller [Authorize] handles it)
        if (attribute == null)
            return await next();

        // Always allow through in Local/Development — no token required
        if (_env.IsEnvironment("Local") || _env.IsDevelopment())
            return await next();

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("User is not authenticated.");

        // Extract roles from JWT claims
        var roles = httpContext.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role
                     || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        if (attribute.IsAllowed(roles))
            return await next();

        throw new UnauthorizedAccessException(
            $"User is not authorized to perform '{typeof(TRequest).Name}'."
        );
    }
}
