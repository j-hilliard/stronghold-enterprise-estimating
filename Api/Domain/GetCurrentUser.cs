using MediatR;
using Stronghold.EnterpriseEstimating.Api.Authorization;
using Stronghold.EnterpriseEstimating.Api.Domain.Users;
using Stronghold.EnterpriseEstimating.Api.Models;
using Stronghold.EnterpriseEstimating.Shared.Enumerations;

namespace Stronghold.EnterpriseEstimating.Api.Domain;

[AllowedAuthorizationRole(
    AuthorizationRole.Administrator,
    AuthorizationRole.User,
    AuthorizationRole.AuthenticatedUser
)]
public class GetCurrentUser : IRequest<User?> { }

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUser, User?>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMediator _mediator;

    public GetCurrentUserHandler(
        IHttpContextAccessor httpContextAccessor,
        IMediator mediator
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
    }

    public async Task<User?> Handle(GetCurrentUser request, CancellationToken cancellationToken)
    {
        var contextUser = _httpContextAccessor.HttpContext?.User;

        if (
            contextUser == null
            || contextUser.Identity == null
            || contextUser.Identity.IsAuthenticated == false
        )
        {
            throw new InvalidOperationException("No logged-in user found.");
        }

        return await _mediator.Send(
            new GetUserByClaimsPrincipal { ClaimsPrincipal = contextUser },
            cancellationToken
        );
    }
}
