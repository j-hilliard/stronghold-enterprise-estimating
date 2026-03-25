using AutoMapper;
using Stronghold.EnterpriseEstimating.Shared.Attributes;

namespace Stronghold.EnterpriseEstimating.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;

    [Sensitive]
    public string? FirstName { get; set; }

    [Sensitive]
    public string? LastName { get; set; }

    [Sensitive]
    public string? Email { get; set; }

    public bool Active { get; set; } = true;
    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedOn { get; set; } = DateTimeOffset.UtcNow;

    public List<UserRole> Roles { get; set; } = new();
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<Data.Models.User, User>()
            .ForMember(u => u.Roles, opt => opt.Ignore());

        CreateMap<Data.Models.UserRole, UserRole>();
        CreateMap<Data.Models.Role, Role>();
    }
}
