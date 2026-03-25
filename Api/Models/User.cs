using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Shared.Attributes;

namespace Stronghold.EnterpriseEstimating.Api.Models;

public class User
{
    public int UserId { get; set; }
    public Guid AzureAdObjectId { get; set; }

    [Sensitive]
    public string? FirstName { get; set; }

    [Sensitive]
    public string? LastName { get; set; }

    [Sensitive]
    public string? Email { get; set; }

    public Guid? CompanyId { get; set; }
    public Guid? RegionId { get; set; }

    [Sensitive]
    public string? CompanyName { get; set; }

    [Sensitive]
    public string? RegionName { get; set; }

    [Sensitive]
    public string? Title { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset? DisabledOn { get; set; }
    public int? DisabledBy { get; set; }
    public string? DisabledReason { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public List<UserRole> Roles { get; set; } = new();

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, Data.Models.User>()
            .ForMember(user => user.UserId, expression => expression.Ignore())
            .ForMember(user => user.CompanyId, expression => expression.MapFrom(source => source.CompanyId))
            .ForMember(user => user.RegionId, expression => expression.MapFrom(source => source.RegionId))
            .ForMember(user => user.CompanyEntity, expression => expression.Ignore())
            .ForMember(user => user.RegionEntity, expression => expression.Ignore())
            .ForMember(user => user.UserRoles, expression => expression.Ignore());

        CreateMap<Data.Models.User, User>()
            .ForMember(user => user.CompanyId, expression => expression.MapFrom(source => source.CompanyId))
            .ForMember(user => user.RegionId, expression => expression.MapFrom(source => source.RegionId))
            .ForMember(
                user => user.CompanyName,
                expression => expression.MapFrom(
                    source => source.CompanyEntity != null ? source.CompanyEntity.Name : null
                )
            )
            .ForMember(
                user => user.RegionName,
                expression => expression.MapFrom(
                    source => source.RegionEntity != null ? source.RegionEntity.Name : null
                )
            )
            .ForMember(user => user.Roles, expression => expression.Ignore());

        CreateMap<UserRole, Data.Models.UserRole>();
        CreateMap<Data.Models.UserRole, UserRole>();

        CreateMap<Role, Data.Models.Role>();
        CreateMap<Data.Models.Role, Role>();
    }
}

public class UserValidator : AbstractValidator<User>
{
    public UserValidator(IMediator mediator, AppDbContext dbContext)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(user => user.AzureAdObjectId)
            .NotEmpty()
            .WithMessage("AzureAdObjectId must not be empty.")
            .NotNull()
            .WithMessage("AzureAdObjectId must not be null.");

        RuleFor(user => user.Active).NotNull().WithMessage("Active status must be specified.");

        RuleFor(user => user.RegionId)
            .Null()
            .When(user => user.CompanyId == null)
            .WithMessage("Region cannot be selected unless a Company is selected.");

        When(
            user => user.CompanyId.HasValue,
            () =>
                RuleFor(user => user.CompanyId)
                    .MustAsync(
                        async (companyId, cancellationToken) =>
                            companyId.HasValue
                            && await dbContext.Companies.AnyAsync(
                                company => company.Id == companyId.Value && company.IsActive,
                                cancellationToken
                            )
                    )
                    .WithMessage("Selected Company is invalid.")
        );

        When(
            user => user.RegionId.HasValue,
            () =>
                RuleFor(user => user)
                    .MustAsync(
                        async (user, cancellationToken) =>
                            await dbContext.Regions.AnyAsync(
                                region =>
                                    region.Id == user.RegionId
                                    && region.IsActive
                                    && region.CompanyId == user.CompanyId,
                                cancellationToken
                            )
                    )
                    .WithMessage("Selected Region does not belong to the selected Company.")
        );
    }
}
