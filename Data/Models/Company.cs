namespace Stronghold.EnterpriseEstimating.Data.Models;

public class Company
{
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? JobLetter { get; set; }
    public string? OpuNumber { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
}
