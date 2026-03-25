using AutoMapper;

namespace Stronghold.EnterpriseEstimating.Api.Models;

public class UserProfileSettings
{
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string? DefaultCompanyCode { get; set; }
    public string? DefaultCustomer { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

public class UserProfileSettingsProfile : Profile
{
    public UserProfileSettingsProfile()
    {
        CreateMap<Data.Models.UserProfileSettings, UserProfileSettings>();
        CreateMap<UserProfileSettings, Data.Models.UserProfileSettings>()
            .ForMember(p => p.User, opt => opt.Ignore());
    }
}
