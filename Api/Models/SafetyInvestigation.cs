namespace Stronghold.EnterpriseEstimating.Api.Models;

public class SafetyInvestigation
{
    public string InvestigationId { get; set; } = string.Empty;

    public string InvestigationNumber { get; set; } = string.Empty;

    public string IncidentId { get; set; } = string.Empty;

    public string IncidentNumber { get; set; } = string.Empty;

    public string IncidentDate { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string OpenDate { get; set; } = string.Empty;

    public string DueDate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string ReviewStatus { get; set; } = string.Empty;

    public string ClassificationStatus { get; set; } = string.Empty;

    public string InvestigationDetails { get; set; } = string.Empty;

    public string IncidentStatus { get; set; } = string.Empty;
}

public class CreateSafetyInvestigationRequest
{
    public Guid IncidentId { get; set; }

    public string? ReviewStatus { get; set; }

    public string? ClassificationStatus { get; set; }

    public string? InvestigationDetails { get; set; }
}

public class UpdateSafetyInvestigationRequest
{
    public string? Status { get; set; }

    public string? ReviewStatus { get; set; }

    public string? ClassificationStatus { get; set; }

    public string? InvestigationDetails { get; set; }
}

public class InvestigationAttributeOption
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;
}

public class InvestigationActionItem
{
    public string ActionId { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string ActionDescription { get; set; } = string.Empty;

    public string AssignedTo { get; set; } = string.Empty;

    public string? DueDate { get; set; }
}

public class TransitionInvestigationRequest
{
    /// <summary>start | complete | approve | reject</summary>
    public string Action { get; set; } = string.Empty;
}

public class CreateInvestigationActionRequest
{
    public string ActionType { get; set; } = string.Empty;

    public string ActionDescription { get; set; } = string.Empty;

    public string AssignedTo { get; set; } = string.Empty;

    public string? DueDate { get; set; }
}
