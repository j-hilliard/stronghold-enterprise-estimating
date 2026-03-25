namespace Stronghold.EnterpriseEstimating.Data.Models;

public class EstimateSummary
{
    public int EstimateSummaryId { get; set; }
    public int EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public decimal BillSubtotal { get; set; }

    // Discount
    public string DiscountType { get; set; } = "None"; // None, Percentage, Dollar
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }

    // Tax
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    // Internal (Job Cost Analysis)
    public decimal InternalCostTotal { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPct { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
