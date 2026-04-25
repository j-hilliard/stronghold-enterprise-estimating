using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user, IEnumerable<string> roles);
    string GenerateToken(User user, string companyCode, IEnumerable<string> roles);
    string GenerateTempToken(User user);
    int? ValidateTempToken(string tempToken);
}
