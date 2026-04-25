using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public class ProposalPdfService
{
    public byte[] GeneratePdf(Estimate estimate)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, estimate));
                page.Footer().Element(ComposeFooter);
            });
        });

        return doc.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .PaddingBottom(10)
            .BorderBottom(1)
            .BorderColor(Colors.Blue.Darken3)
            .Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("STRONGHOLD COMPANIES").Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                    col.Item().Text("Enterprise Estimating").FontSize(10).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text("PROPOSAL").Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                    col.Item().Text($"Date: {DateTime.Now:MMMM d, yyyy}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
    }

    private void ComposeContent(IContainer container, Estimate estimate)
    {
        container.Column(col =>
        {
            col.Spacing(16);

            // Estimate info block
            col.Item().PaddingTop(10).Table(t =>
            {
                t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });

                void Cell(string label, string? value) =>
                    t.Cell().Column(inner =>
                    {
                        inner.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1).Bold();
                        inner.Item().Text(value ?? "—").FontSize(10);
                    });

                Cell("ESTIMATE NUMBER", estimate.EstimateNumber);
                Cell("CLIENT", estimate.Client);
                Cell("JOB NAME", estimate.Name);
                Cell("JOB TYPE", estimate.JobType ?? "—");
                Cell("LOCATION", string.Join(", ", new[] { estimate.City, estimate.State }.Where(x => !string.IsNullOrEmpty(x))));
                Cell("SITE / FACILITY", estimate.Site ?? "—");
                Cell("START DATE", estimate.StartDate?.ToString("MMMM d, yyyy") ?? "—");
                Cell("END DATE", estimate.EndDate?.ToString("MMMM d, yyyy") ?? "—");
                Cell("DURATION", estimate.Days > 0 ? $"{estimate.Days} days" : "—");
                Cell("SHIFT", estimate.Shift);
            });

            // Scope summary table
            col.Item().Element(c => ComposeScopeTable(c, estimate));

            // Terms & conditions
            col.Item().Element(ComposeTerms);

            // Exclusions note
            col.Item()
                .Background(Colors.Grey.Lighten3)
                .Padding(8)
                .Text(text =>
                {
                    text.Span("Note: ").Bold();
                    text.Span("This proposal does not include internal cost rates, burden rates, or gross margin data.");
                });
        });
    }

    private void ComposeScopeTable(IContainer container, Estimate estimate)
    {
        var summary = estimate.Summary;
        var laborTotal = estimate.LaborRows?.Sum(r => r.Subtotal) ?? 0;
        var equipTotal = estimate.EquipmentRows?.Sum(r => r.Subtotal) ?? 0;
        var expenseTotal = estimate.ExpenseRows?.Sum(r => r.Subtotal) ?? 0;
        var grandTotal = summary?.GrandTotal ?? (laborTotal + equipTotal + expenseTotal);

        container.Column(col =>
        {
            col.Item().Text("SCOPE SUMMARY").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(4).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(1);
                });

                // Header
                t.Header(h =>
                {
                    h.Cell().Background(Colors.Blue.Darken3).Padding(6)
                        .Text("DESCRIPTION").FontColor(Colors.White).Bold().FontSize(9);
                    h.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight()
                        .Text("AMOUNT").FontColor(Colors.White).Bold().FontSize(9);
                });

                void Row(string label, decimal amount, bool isTotal = false)
                {
                    var bg = isTotal ? Colors.Blue.Lighten4 : Colors.White;
                    var fs = isTotal ? 11 : 10;
                    if (isTotal)
                    {
                        t.Cell().Background(bg).Padding(6).Text(label).Bold().FontSize(fs);
                        t.Cell().Background(bg).Padding(6).AlignRight().Text(amount.ToString("C0")).Bold().FontSize(fs);
                    }
                    else
                    {
                        t.Cell().Background(bg).Padding(6).Text(label).FontSize(fs);
                        t.Cell().Background(bg).Padding(6).AlignRight().Text(amount.ToString("C0")).FontSize(fs);
                    }
                }

                Row("Labor", laborTotal);
                Row("Equipment", equipTotal);
                Row("Expenses / Per Diem", expenseTotal);

                if (summary?.DiscountAmount > 0)
                    Row($"Discount ({summary.DiscountType})", -(summary.DiscountAmount));

                if (summary?.TaxAmount > 0)
                    Row($"Tax ({summary.TaxRate:P1})", summary.TaxAmount);

                Row("GRAND TOTAL", grandTotal, isTotal: true);
            });
        });
    }

    private void ComposeTerms(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("TERMS & CONDITIONS").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(4).Text(text =>
            {
                text.Span("Validity: ").Bold();
                text.Span("This proposal is valid for 30 days from the date of issue.\n");
                text.Span("Payment Terms: ").Bold();
                text.Span("Net 30 days from invoice date unless otherwise agreed in writing.\n");
                text.Span("Scope: ").Bold();
                text.Span("This proposal covers only the work described herein. Any additions or changes to scope will require a written change order.\n");
                text.Span("Safety: ").Bold();
                text.Span("All work shall be performed in accordance with applicable federal, state, and local safety regulations and client site requirements.");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container
            .PaddingTop(8)
            .BorderTop(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Row(row =>
            {
                row.RelativeItem().Text("Stronghold Companies — Confidential").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(100).AlignRight()
                    .Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                        x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
            });
    }
}
