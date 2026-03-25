using AutoMapper;
using FluentValidation;
using MediatR;

namespace Stronghold.EnterpriseEstimating.Api.Models;

public class Permission
{
    public int PermissionId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
}

public class PermissionProfile : Profile
{
    public PermissionProfile()
    {
        CreateMap<Data.Models.Permission, Permission>().ReverseMap();
    }
}

public class PermissionValidator : AbstractValidator<Permission>
{
    public PermissionValidator(IMediator mediator)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(permission => permission.Code).NotEmpty();
        RuleFor(permission => permission.Name).NotEmpty();
    }
}
