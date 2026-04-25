namespace Stronghold.EnterpriseEstimating.Data.Models;

public class UserCompany
{
    public int UserId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    public User? User { get; set; }
    public Company? Company { get; set; }
}
