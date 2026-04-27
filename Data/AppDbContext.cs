using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCompany> UserCompanies { get; set; } = null!;
    public DbSet<UserProfileSettings> Profiles { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<Settings> Settings { get; set; } = null!;

    // Estimating
    public DbSet<Estimate> Estimates { get; set; } = null!;
    public DbSet<EstimateRevision> EstimateRevisions { get; set; } = null!;
    public DbSet<EstimateSummary> EstimateSummaries { get; set; } = null!;
    public DbSet<LaborRow> LaborRows { get; set; } = null!;
    public DbSet<EquipmentRow> EquipmentRows { get; set; } = null!;
    public DbSet<ExpenseRow> ExpenseRows { get; set; } = null!;
    public DbSet<FcoEntry> FcoEntries { get; set; } = null!;
    public DbSet<EstimateSequence> EstimateSequences { get; set; } = null!;

    // Staffing Plans
    public DbSet<StaffingPlan> StaffingPlans { get; set; } = null!;
    public DbSet<StaffingLaborRow> StaffingLaborRows { get; set; } = null!;

    // Rate Books
    public DbSet<RateBook> RateBooks { get; set; } = null!;
    public DbSet<RateBookLaborRate> RateBookLaborRates { get; set; } = null!;
    public DbSet<RateBookEquipmentRate> RateBookEquipmentRates { get; set; } = null!;
    public DbSet<RateBookExpenseItem> RateBookExpenseItems { get; set; } = null!;
    public DbSet<CrewTemplate> CrewTemplates { get; set; } = null!;
    public DbSet<CrewTemplateRow> CrewTemplateRows { get; set; } = null!;

    // Cost Books
    public DbSet<CostBook> CostBooks { get; set; } = null!;
    public DbSet<CostBookLaborRate> CostBookLaborRates { get; set; } = null!;
    public DbSet<CostBookEquipmentRate> CostBookEquipmentRates { get; set; } = null!;
    public DbSet<CostBookExpense> CostBookExpenses { get; set; } = null!;
    public DbSet<CostBookOverheadItem> CostBookOverheadItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(b =>
        {
            b.HasKey(c => c.CompanyCode);
            b.Property(c => c.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(c => c.Name).IsRequired().HasMaxLength(200);
            b.Property(c => c.ShortName).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<UserCompany>(b =>
        {
            b.HasKey(uc => new { uc.UserId, uc.CompanyCode });
            b.HasOne(uc => uc.User)
                .WithMany(u => u.UserCompanies)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(uc => uc.Company)
                .WithMany(c => c.UserCompanies)
                .HasForeignKey(uc => uc.CompanyCode)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.UserId);
            b.HasIndex(u => u.Username).IsUnique();
            b.Property(u => u.Username).IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.CompanyCode).IsRequired().HasMaxLength(10);
        });

        modelBuilder.Entity<UserProfileSettings>(b =>
        {
            b.ToTable("Profile");
            b.HasKey(p => p.ProfileId);
            b.HasIndex(p => p.UserId).IsUnique();
            b.HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfileSettings>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(r => r.RoleId);
            b.Property(r => r.Name).IsRequired();
            b.Property(r => r.Description).IsRequired();
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.HasKey(p => p.PermissionId);
            b.Property(p => p.Code).IsRequired();
            b.Property(p => p.Name).IsRequired();
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            b.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Settings>(b =>
        {
            b.HasKey(s => s.SettingsId);
        });

        // ── Estimates ──────────────────────────────────────────────────────
        modelBuilder.Entity<Estimate>(b =>
        {
            b.HasKey(e => e.EstimateId);
            b.HasIndex(e => new { e.CompanyCode, e.EstimateNumber }).IsUnique();
            b.Property(e => e.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(e => e.EstimateNumber).IsRequired().HasMaxLength(50);
            b.Property(e => e.Name).IsRequired().HasMaxLength(200);
            b.Property(e => e.Client).IsRequired().HasMaxLength(200);
            b.Property(e => e.Status).IsRequired().HasMaxLength(30);
            b.Property(e => e.Shift).IsRequired().HasMaxLength(10);
            b.Property(e => e.OtMethod).IsRequired().HasMaxLength(30);
            b.Property(e => e.ConfidencePct).HasPrecision(5, 2);
            b.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

            b.HasOne(e => e.StaffingPlan)
                .WithMany(sp => sp.Estimates)
                .HasForeignKey(e => e.StaffingPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EstimateRevision>(b =>
        {
            b.HasKey(r => r.EstimateRevisionId);
            b.HasIndex(r => new { r.EstimateId, r.RevisionNumber }).IsUnique();
            b.Property(r => r.LaborTotal).HasPrecision(18, 2);
            b.Property(r => r.EquipTotal).HasPrecision(18, 2);
            b.Property(r => r.GrandTotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.Revisions)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EstimateSummary>(b =>
        {
            b.HasKey(s => s.EstimateSummaryId);
            b.HasIndex(s => s.EstimateId).IsUnique();
            b.Property(s => s.BillSubtotal).HasPrecision(18, 2);
            b.Property(s => s.DiscountValue).HasPrecision(18, 4);
            b.Property(s => s.DiscountAmount).HasPrecision(18, 2);
            b.Property(s => s.TaxRate).HasPrecision(5, 4);
            b.Property(s => s.TaxAmount).HasPrecision(18, 2);
            b.Property(s => s.GrandTotal).HasPrecision(18, 2);
            b.Property(s => s.InternalCostTotal).HasPrecision(18, 2);
            b.Property(s => s.GrossProfit).HasPrecision(18, 2);
            b.Property(s => s.GrossMarginPct).HasPrecision(18, 4);
            b.HasOne(s => s.Estimate)
                .WithOne(e => e.Summary)
                .HasForeignKey<EstimateSummary>(s => s.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LaborRow>(b =>
        {
            b.HasKey(r => r.LaborRowId);
            b.Property(r => r.BillStRate).HasPrecision(18, 4);
            b.Property(r => r.BillOtRate).HasPrecision(18, 4);
            b.Property(r => r.BillDtRate).HasPrecision(18, 4);
            b.Property(r => r.StHours).HasPrecision(10, 2);
            b.Property(r => r.OtHours).HasPrecision(10, 2);
            b.Property(r => r.DtHours).HasPrecision(10, 2);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.LaborRows)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EquipmentRow>(b =>
        {
            b.HasKey(r => r.EquipmentRowId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.EquipmentRows)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseRow>(b =>
        {
            b.HasKey(r => r.ExpenseRowId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.ExpenseRows)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FcoEntry>(b =>
        {
            b.HasKey(f => f.FcoEntryId);
            b.Property(f => f.DollarAdjustment).HasPrecision(18, 2);
            b.HasOne(f => f.Estimate)
                .WithMany(e => e.FcoEntries)
                .HasForeignKey(f => f.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EstimateSequence>(b =>
        {
            b.HasKey(s => s.EstimateSequenceId);
            b.HasIndex(s => new { s.CompanyCode, s.Year, s.SequenceType }).IsUnique();
        });

        // ── Staffing Plans ─────────────────────────────────────────────────
        modelBuilder.Entity<StaffingPlan>(b =>
        {
            b.HasKey(sp => sp.StaffingPlanId);
            b.HasIndex(sp => new { sp.CompanyCode, sp.StaffingPlanNumber }).IsUnique();
            b.Property(sp => sp.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(sp => sp.StaffingPlanNumber).IsRequired().HasMaxLength(50);
            b.Property(sp => sp.Name).IsRequired().HasMaxLength(200);
            b.Property(sp => sp.Client).IsRequired().HasMaxLength(200);
            b.Property(sp => sp.RoughLaborTotal).HasPrecision(18, 2);

            b.HasOne(sp => sp.ConvertedEstimate)
                .WithMany()
                .HasForeignKey(sp => sp.ConvertedEstimateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StaffingLaborRow>(b =>
        {
            b.HasKey(r => r.StaffingLaborRowId);
            b.Property(r => r.StRate).HasPrecision(18, 4);
            b.Property(r => r.OtRate).HasPrecision(18, 4);
            b.Property(r => r.DtRate).HasPrecision(18, 4);
            b.Property(r => r.StHours).HasPrecision(10, 2);
            b.Property(r => r.OtHours).HasPrecision(10, 2);
            b.Property(r => r.DtHours).HasPrecision(10, 2);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.StaffingPlan)
                .WithMany(sp => sp.LaborRows)
                .HasForeignKey(r => r.StaffingPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Rate Books ─────────────────────────────────────────────────────
        modelBuilder.Entity<RateBook>(b =>
        {
            b.HasKey(rb => rb.RateBookId);
            b.Property(rb => rb.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(rb => rb.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<RateBookLaborRate>(b =>
        {
            b.HasKey(r => r.RateBookLaborRateId);
            b.Property(r => r.StRate).HasPrecision(18, 4);
            b.Property(r => r.OtRate).HasPrecision(18, 4);
            b.Property(r => r.DtRate).HasPrecision(18, 4);
            b.HasOne(r => r.RateBook)
                .WithMany(rb => rb.LaborRates)
                .HasForeignKey(r => r.RateBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RateBookEquipmentRate>(b =>
        {
            b.HasKey(r => r.RateBookEquipmentRateId);
            b.Property(r => r.Hourly).HasPrecision(18, 4);
            b.Property(r => r.Daily).HasPrecision(18, 4);
            b.Property(r => r.Weekly).HasPrecision(18, 4);
            b.Property(r => r.Monthly).HasPrecision(18, 4);
            b.HasOne(r => r.RateBook)
                .WithMany(rb => rb.EquipmentRates)
                .HasForeignKey(r => r.RateBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RateBookExpenseItem>(b =>
        {
            b.HasKey(r => r.RateBookExpenseItemId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.HasOne(r => r.RateBook)
                .WithMany(rb => rb.ExpenseItems)
                .HasForeignKey(r => r.RateBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CrewTemplate>(b =>
        {
            b.HasKey(ct => ct.CrewTemplateId);
            b.Property(ct => ct.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(ct => ct.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<CrewTemplateRow>(b =>
        {
            b.HasKey(r => r.CrewTemplateRowId);
            b.HasOne(r => r.CrewTemplate)
                .WithMany(ct => ct.Rows)
                .HasForeignKey(r => r.CrewTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Cost Books ─────────────────────────────────────────────────────
        modelBuilder.Entity<CostBook>(b =>
        {
            b.HasKey(cb => cb.CostBookId);
            b.Property(cb => cb.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(cb => cb.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<CostBookLaborRate>(b =>
        {
            b.HasKey(r => r.CostBookLaborRateId);
            b.Property(r => r.StRate).HasPrecision(18, 4);
            b.Property(r => r.OtRate).HasPrecision(18, 4);
            b.Property(r => r.DtRate).HasPrecision(18, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.LaborRates)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostBookEquipmentRate>(b =>
        {
            b.HasKey(r => r.CostBookEquipmentRateId);
            b.Property(r => r.Hourly).HasPrecision(18, 4);
            b.Property(r => r.Daily).HasPrecision(18, 4);
            b.Property(r => r.Weekly).HasPrecision(18, 4);
            b.Property(r => r.Monthly).HasPrecision(18, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.EquipmentRates)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostBookExpense>(b =>
        {
            b.HasKey(r => r.CostBookExpenseId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.Expenses)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostBookOverheadItem>(b =>
        {
            b.HasKey(r => r.CostBookOverheadItemId);
            b.Property(r => r.Value).HasPrecision(10, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.OverheadItems)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ApplyAuditInfo()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity is User u)
            {
                u.ModifiedOn = now;
                if (entry.State == EntityState.Added) u.CreatedOn = now;
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }
}
