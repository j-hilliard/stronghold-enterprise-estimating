using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user, IEnumerable<string> roles);
}
