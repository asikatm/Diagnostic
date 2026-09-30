using System.Linq.Expressions;
using Diagnostic.Api.Common;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Data;

public class DiagnosticDbContext(DbContextOptions<DiagnosticDbContext> options, ICurrentUser? currentUser = null)
    : DbContext(options)
{
    public const string AdminSchema = "admin";
    public const string DiagSchema = "diag";

    // ---------- admin ----------
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ServiceUnit> ServiceUnits => Set<ServiceUnit>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuPermission> MenuPermissions => Set<MenuPermission>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportSpParameter> ReportSpParameters => Set<ReportSpParameter>();
    public DbSet<ReportPermission> ReportPermissions => Set<ReportPermission>();
    public DbSet<PaymentMode> PaymentModes => Set<PaymentMode>();
    public DbSet<PaymentChannel> PaymentChannels => Set<PaymentChannel>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Religion> Religions => Set<Religion>();
    public DbSet<AppGlobalSetting> AppGlobalSettings => Set<AppGlobalSetting>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    // ---------- diag ----------
    public DbSet<ParentGroup> ParentGroups => Set<ParentGroup>();
    public DbSet<TestGroup> TestGroups => Set<TestGroup>();
    public DbSet<Specimen> Specimens => Set<Specimen>();
    public DbSet<Vacutainer> Vacutainers => Set<Vacutainer>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<TestItem> TestItems => Set<TestItem>();
    public DbSet<ReportParameter> ReportParameters => Set<ReportParameter>();
    public DbSet<VacuMachineMapping> VacuMachineMappings => Set<VacuMachineMapping>();
    public DbSet<ReportDeliveryTime> ReportDeliveryTimes => Set<ReportDeliveryTime>();
    public DbSet<OpdPackage> OpdPackages => Set<OpdPackage>();
    public DbSet<OpdPackageItem> OpdPackageItems => Set<OpdPackageItem>();
    public DbSet<LetterHead> LetterHeads => Set<LetterHead>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DiscountSetup> DiscountSetups => Set<DiscountSetup>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Investigation> Investigations => Set<Investigation>();
    public DbSet<InvestigationDetail> InvestigationDetails => Set<InvestigationDetail>();
    public DbSet<InvestigationPayment> InvestigationPayments => Set<InvestigationPayment>();
    public DbSet<CancelRequest> CancelRequests => Set<CancelRequest>();
    public DbSet<SampleCollection> SampleCollections => Set<SampleCollection>();
    public DbSet<LabReport> LabReports => Set<LabReport>();
    public DbSet<LabResult> LabResults => Set<LabResult>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<string>().HaveMaxLength(250);
        builder.Properties<decimal>().HavePrecision(18, 2);
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        MapAdmin(b);
        MapDiag(b);

        foreach (var entity in b.Model.GetEntityTypes())
        {
            // No accidental cascade deletes – only explicitly configured children cascade.
            foreach (var fk in entity.GetForeignKeys().Where(f => f.DeleteBehavior == DeleteBehavior.Cascade && !f.IsOwnership))
                if (!CascadeChildren.Contains(entity.ClrType)) fk.DeleteBehavior = DeleteBehavior.Restrict;

            if (typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
                entity.SetQueryFilter(SoftDeleteFilter(entity.ClrType));
        }
    }

    private static readonly HashSet<Type> CascadeChildren =
    [
        typeof(UserRole), typeof(MenuPermission), typeof(ReportSpParameter), typeof(ReportPermission),
        typeof(ReportParameter), typeof(OpdPackageItem), typeof(InvestigationDetail),
        typeof(InvestigationPayment), typeof(LabResult),
    ];

    private static LambdaExpression SoftDeleteFilter(Type type)
    {
        var p = Expression.Parameter(type, "e");
        var body = Expression.Not(Expression.Property(p, nameof(BaseEntity.IsDeleted)));
        return Expression.Lambda(body, p);
    }

    // ------------------------------------------------------------------ admin schema
    private static void MapAdmin(ModelBuilder b)
    {
        const string s = AdminSchema;

        b.Entity<Tenant>(e => { e.ToTable("Tenants", s); UniqueCode(e); e.Property(x => x.Name).HasMaxLength(150); });
        b.Entity<Department>(e => { e.ToTable("Departments", s); UniqueCode(e); });
        b.Entity<ServiceUnit>(e => { e.ToTable("ServiceUnits", s); UniqueCode(e); });

        b.Entity<Role>(e =>
        {
            e.ToTable("Roles", s);
            UniqueCode(e);
            e.HasIndex(x => x.Name).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<User>(e =>
        {
            e.ToTable("Users", s);
            e.Property(x => x.UserName).HasMaxLength(50);
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.PasswordHash).HasMaxLength(500);
            e.HasIndex(x => x.UserName).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles", s);
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Project>(e => { e.ToTable("Projects", s); UniqueCode(e); });

        b.Entity<Menu>(e =>
        {
            e.ToTable("Menus", s);
            UniqueCode(e);
            e.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MenuPermission>(e =>
        {
            e.ToTable("MenuPermissions", s);
            e.HasIndex(x => new { x.RoleId, x.MenuId }).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Menu).WithMany().HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Report>(e =>
        {
            e.ToTable("Reports", s);
            UniqueCode(e);
            e.Property(x => x.ProcedureName).HasMaxLength(128);
            e.Property(x => x.Template).HasMaxLength(50);
            e.Property(x => x.Orientation).HasMaxLength(20);
        });

        b.Entity<ReportSpParameter>(e =>
        {
            e.ToTable("ReportSpParameters", s);
            e.Property(x => x.ParamName).HasMaxLength(128);
            e.Property(x => x.DataType).HasMaxLength(20);
            e.HasOne(x => x.Report).WithMany(x => x.Parameters).HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ReportId, x.ParamName }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<ReportPermission>(e =>
        {
            e.ToTable("ReportPermissions", s);
            e.HasIndex(x => new { x.RoleId, x.ReportId }).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Report).WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PaymentMode>(e => { e.ToTable("PaymentModes", s); UniqueCode(e); });
        b.Entity<PaymentChannel>(e => { e.ToTable("PaymentChannels", s); UniqueCode(e); });
        b.Entity<Country>(e => { e.ToTable("Countries", s); UniqueCode(e); });
        b.Entity<Religion>(e => { e.ToTable("Religions", s); UniqueCode(e); });

        b.Entity<AppGlobalSetting>(e =>
        {
            e.ToTable("AppGlobalSettings", s);
            e.Property(x => x.SettingKey).HasMaxLength(100);
            e.Property(x => x.SettingValue).HasColumnType("nvarchar(max)");
            e.HasIndex(x => x.SettingKey).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<Printer>(e => { e.ToTable("Printers", s); UniqueCode(e); });

        b.Entity<NumberSequence>(e =>
        {
            e.ToTable("NumberSequences", s);
            e.Property(x => x.SequenceKey).HasMaxLength(30);
            e.Property(x => x.Prefix).HasMaxLength(10);
            e.Property(x => x.Period).HasMaxLength(10);
            e.HasIndex(x => x.SequenceKey).IsUnique();
        });
    }

    // ------------------------------------------------------------------ diag schema
    private static void MapDiag(ModelBuilder b)
    {
        const string s = DiagSchema;

        b.Entity<ParentGroup>(e => { e.ToTable("ParentGroups", s); UniqueCode(e); });
        b.Entity<TestGroup>(e => { e.ToTable("TestGroups", s); UniqueCode(e); });
        b.Entity<Specimen>(e => { e.ToTable("Specimens", s); UniqueCode(e); });
        b.Entity<Vacutainer>(e => { e.ToTable("Vacutainers", s); UniqueCode(e); e.Property(x => x.ColorHex).HasMaxLength(10); });
        b.Entity<Machine>(e => { e.ToTable("Machines", s); UniqueCode(e); });

        b.Entity<TestItem>(e =>
        {
            e.ToTable("TestItems", s);
            UniqueCode(e);
            e.Property(x => x.DefaultReportText).HasColumnType("nvarchar(max)");
            e.Property(x => x.VatPercent).HasPrecision(5, 2);
            e.Property(x => x.MaxDiscountPercent).HasPrecision(5, 2);
            e.HasIndex(x => x.Name);
        });

        b.Entity<ReportParameter>(e =>
        {
            e.ToTable("ReportParameters", s);
            e.Property(x => x.Options).HasMaxLength(500);
            e.Property(x => x.LowValue).HasPrecision(18, 4);
            e.Property(x => x.HighValue).HasPrecision(18, 4);
            e.HasOne(x => x.TestItem).WithMany(x => x.Parameters).HasForeignKey(x => x.TestItemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VacuMachineMapping>(e =>
        {
            e.ToTable("VacuMachineMappings", s);
            e.HasIndex(x => new { x.TestGroupId, x.TestItemId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<ReportDeliveryTime>(e => e.ToTable("ReportDeliveryTimes", s));

        b.Entity<OpdPackage>(e => { e.ToTable("OpdPackages", s); UniqueCode(e); });
        b.Entity<OpdPackageItem>(e =>
        {
            e.ToTable("OpdPackageItems", s);
            e.HasOne(x => x.OpdPackage).WithMany(x => x.Items).HasForeignKey(x => x.OpdPackageId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LetterHead>(e => { e.ToTable("LetterHeads", s); UniqueCode(e); e.Property(x => x.FooterText).HasMaxLength(1000); });
        b.Entity<Doctor>(e => { e.ToTable("Doctors", s); UniqueCode(e); e.Property(x => x.CommissionPercent).HasPrecision(5, 2); });
        b.Entity<DiscountSetup>(e => { e.ToTable("DiscountSetups", s); UniqueCode(e); });

        b.Entity<Patient>(e =>
        {
            e.ToTable("Patients", s);
            e.Property(x => x.PatientCode).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.BloodGroup).HasMaxLength(5);
            e.HasIndex(x => x.PatientCode).IsUnique();
            e.HasIndex(x => x.Phone);
        });

        b.Entity<Investigation>(e =>
        {
            e.ToTable("Investigations", s);
            e.Property(x => x.InvoiceNo).HasMaxLength(20);
            e.Property(x => x.PatientAge).HasMaxLength(30);
            e.HasIndex(x => x.InvoiceNo).IsUnique();
            e.HasIndex(x => x.InvoiceDate);
        });

        b.Entity<InvestigationDetail>(e =>
        {
            e.ToTable("InvestigationDetails", s);
            e.HasOne(x => x.Investigation).WithMany(x => x.Details).HasForeignKey(x => x.InvestigationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SampleCollection).WithMany(x => x.Details).HasForeignKey(x => x.SampleCollectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Status);
        });

        b.Entity<InvestigationPayment>(e =>
        {
            e.ToTable("InvestigationPayments", s);
            e.Property(x => x.ReceiptNo).HasMaxLength(20);
            e.HasIndex(x => x.ReceiptNo).IsUnique();
            e.HasIndex(x => x.PaymentDate);
            e.HasOne(x => x.Investigation).WithMany(x => x.Payments).HasForeignKey(x => x.InvestigationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CancelRequest>(e =>
        {
            e.ToTable("CancelRequests", s);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ApprovalRemarks).HasMaxLength(500);
            e.HasIndex(x => x.Status);
        });

        b.Entity<SampleCollection>(e =>
        {
            e.ToTable("SampleCollections", s);
            e.Property(x => x.SampleNo).HasMaxLength(20);
            e.HasIndex(x => x.SampleNo).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.RepeatOfSample).WithMany().HasForeignKey(x => x.RepeatOfSampleId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<LabReport>(e =>
        {
            e.ToTable("LabReports", s);
            e.Property(x => x.ReportText).HasColumnType("nvarchar(max)");
            e.Property(x => x.Comments).HasMaxLength(2000);
            e.Property(x => x.ReturnReason).HasMaxLength(500);
            e.HasIndex(x => x.InvestigationDetailId).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<LabResult>(e =>
        {
            e.ToTable("LabResults", s);
            e.Property(x => x.ResultValue).HasMaxLength(500);
            e.HasOne(x => x.LabReport).WithMany(x => x.Results).HasForeignKey(x => x.LabReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.LabReportId, x.ReportParameterId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }

    private static void UniqueCode<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e) where T : MasterEntity
    {
        e.Property(x => x.Code).HasMaxLength(30);
        e.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL AND [IsDeleted] = 0");
    }

    // ------------------------------------------------------------------ audit
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        ApplyAudit();
        return base.SaveChanges();
    }

    private void ApplyAudit()
    {
        var now = DateTime.Now;
        var userId = currentUser?.UserId;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= userId;
                    break;
                case EntityState.Modified:
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    // Soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
