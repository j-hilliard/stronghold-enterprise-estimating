namespace Stronghold.EnterpriseEstimating.Shared.Enumerations;

public enum AuthorizationRole
{
    User,
    Administrator,
    Estimator,
    Viewer,
    Analytics,
    AuthenticatedUser,
}

public static class AuthorizationRoles
{
    public const string Administrator = nameof(AuthorizationRole.Administrator);
    public const string Estimator = nameof(AuthorizationRole.Estimator);
    public const string Viewer = nameof(AuthorizationRole.Viewer);
    public const string Analytics = nameof(AuthorizationRole.Analytics);
    public const string User = nameof(AuthorizationRole.User);
}
