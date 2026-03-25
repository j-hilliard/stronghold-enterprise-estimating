using AutoMapper;

namespace Stronghold.EnterpriseEstimating.Api.Models;

public class UserProfileSettings
{
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string? DefaultDateRange { get; set; }
    public string? DefaultCompany { get; set; }
    public string? DefaultCustomer { get; set; }
    public List<string> DefaultIncidentStatuses { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
    public int? CreatedById { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public int? ModifiedById { get; set; }
}

public class UserProfileSettingsProfile : Profile
{
    public UserProfileSettingsProfile()
    {
        CreateMap<UserProfileSettings, Data.Models.UserProfileSettings>()
            .ForMember(profile => profile.DefaultIncidentStatus, expression => expression.Ignore())
            .ForMember(profile => profile.IncidentStatuses, expression => expression.Ignore())
            .ForMember(profile => profile.User, expression => expression.Ignore())
            .ForMember(profile => profile.CreatedBy, expression => expression.Ignore())
            .ForMember(profile => profile.ModifiedBy, expression => expression.Ignore());

        CreateMap<Data.Models.UserProfileSettings, UserProfileSettings>()
            .ForMember(
                profile => profile.DefaultIncidentStatuses,
                expression => expression.MapFrom(source => source.IncidentStatuses
                    .Select(status => status.IncidentStatus)
                    .Where(status => !string.IsNullOrWhiteSpace(status))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList())
            );
    }
}
