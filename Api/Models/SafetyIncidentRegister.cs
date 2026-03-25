namespace Stronghold.EnterpriseEstimating.Api.Models;

public class SafetyIncidentRegisterSummary
{
    public int TotalIncidents { get; set; }

    public int OccurredLast30Days { get; set; }

    public int OpenInvestigations { get; set; }

    public int OpenInvestigationsOver30Days { get; set; }

    public int RecordableIncidents { get; set; }

    public int LostTimeIncidents { get; set; }

    public int NearMisses { get; set; }
}

public class SafetyIncidentTrendPoint
{
    public string MonthKey { get; set; } = string.Empty;

    public string MonthLabel { get; set; } = string.Empty;

    public int IncidentCount { get; set; }

    public int NearMissCount { get; set; }
}

public class SafetyIncidentRegisterItem
{
    public string IncidentId { get; set; } = string.Empty;

    public string IncidentNumber { get; set; } = string.Empty;

    public string IncidentDate { get; set; } = string.Empty;

    public string CompanyCode { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string RegionCode { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public string CustomerSite { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string IncidentClass { get; set; } = string.Empty;

    public string SeverityActualCode { get; set; } = string.Empty;

    public string SeverityPotentialCode { get; set; } = string.Empty;

    public bool IsRecordable { get; set; }

    public bool IsLostTime { get; set; }

    public bool IsNearMiss { get; set; }
}

public class SafetyIncidentRegisterOption
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class SafetyIncidentRegisterStatusCount
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class SafetyIncidentRegisterPage
{
    public List<SafetyIncidentRegisterItem> Items { get; set; } =
        new List<SafetyIncidentRegisterItem>();

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int PageCount { get; set; }

    public int FilteredOpenCount { get; set; }

    public int FilteredHighSeverityCount { get; set; }

    public List<SafetyIncidentRegisterOption> Companies { get; set; } =
        new List<SafetyIncidentRegisterOption>();

    public List<SafetyIncidentRegisterOption> Customers { get; set; } =
        new List<SafetyIncidentRegisterOption>();

    public List<SafetyIncidentRegisterOption> Statuses { get; set; } =
        new List<SafetyIncidentRegisterOption>();

    public List<SafetyIncidentRegisterStatusCount> StatusCounts { get; set; } =
        new List<SafetyIncidentRegisterStatusCount>();
}
