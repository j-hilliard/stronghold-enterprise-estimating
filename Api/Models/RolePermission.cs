using AutoMapper;
using FluentValidation;
using MediatR;

namespace Stronghold.EnterpriseEstimating.Api.Models;

public class RolePermission
{
    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

public class RolePermissionProfile : Profile
{
    public RolePermissionProfile()
    {
        CreateMap<RolePermission, Data.Models.RolePermission>();
        CreateMap<Data.Models.RolePermission, RolePermission>();
    }
}

public class RolePermissionValidator : AbstractValidator<RolePermission>
{
    public RolePermissionValidator(IMediator mediator)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(rolePermission => rolePermission.RoleId).NotEmpty().GreaterThan(0);
        RuleFor(rolePermission => rolePermission.PermissionId).NotEmpty().GreaterThan(0);
    }
}
