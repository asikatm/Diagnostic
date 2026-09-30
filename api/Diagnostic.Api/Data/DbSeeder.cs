using System.Text.RegularExpressions;
using Diagnostic.Api.Common;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Data;

/// <summary>
/// Applies migrations, (re)creates the report stored procedures and seeds default data.
/// Menus and report mappings are upserted by code on every start; everything else is seeded only on an empty database.
/// </summary>
public class DbSeeder(DiagnosticDbContext db, IPasswordHasher<User> hasher, ILogger<DbSeeder> logger, IWebHostEnvironment env)
{
    public async Task RunAsync()
    {
        await db.Database.MigrateAsync();
        await RunSqlScriptsAsync();

        await SeedProjectsAndMenusAsync();
        await SeedReportsAsync();

        if (!await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("Empty database – seeding default data");
            await SeedAdminAsync();
            await SeedDiagnosticAsync();
        }
    }

    private async Task RunSqlScriptsAsync()
    {
        var dir = Path.Combine(env.ContentRootPath, "Data", "Sql");
        if (!Directory.Exists(dir)) dir = Path.Combine(AppContext.BaseDirectory, "Data", "Sql");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.sql").Order())
        {
            var batches = Regex.Split(await File.ReadAllTextAsync(file), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
                await db.Database.ExecuteSqlRawAsync(batch);
            logger.LogInformation("Executed {File}", Path.GetFileName(file));
        }
    }

    // ------------------------------------------------------------------ projects & menus

    /// <param name="Sort">Explicit sort order (for menus added later between existing ones).</param>
    /// <param name="Roles">Role codes that get full rights when the menu is first created.</param>
    private record MenuSeed(string Code, string Name, string Url, string Icon, int? Sort = null, string[]? Roles = null);

    private static readonly MenuSeed[] DiagnosticMenus =
    [
        new(MenuCodes.DiagDashboard, "Dashboard", "/Diagnostic/Dashboard", "bi-grid-1x2"),
        new(MenuCodes.Investigation, "Investigation Entry", "/Diagnostic/Investigation", "bi-receipt"),
        new(MenuCodes.InvoiceList, "Invoice List / Due Collection", "/Diagnostic/Invoices", "bi-list-check"),
        new(MenuCodes.SampleCollection, "Sample Collection", "/Diagnostic/SampleCollection", "bi-droplet"),
        new(MenuCodes.SampleAck, "Sample Acknowledgement", "/Diagnostic/SampleAcknowledgement", "bi-clipboard-check", 45, ["LAB_TECH", "PATHOLOGIST"]),
        new(MenuCodes.Sticker, "Pathology Sticker Print", "/Diagnostic/Sticker", "bi-upc-scan"),
        new(MenuCodes.ResultEntry, "Result Entry", "/Diagnostic/ResultEntry", "bi-clipboard2-pulse"),
        new(MenuCodes.ReportFinalize, "Lab Report Finalization", "/Diagnostic/ReportFinalization", "bi-patch-check", 65, ["PATHOLOGIST"]),
        new(MenuCodes.LabReport, "LABREPORT WITH LETTER HEAD", "/Diagnostic/LabReport", "bi-file-earmark-medical"),
        new(MenuCodes.ReportDelivery, "Report Delivery", "/Diagnostic/ReportDelivery", "bi-box-arrow-up-right", 75, ["RECEPTION", "ACCOUNTS"]),
        new(MenuCodes.CancelService, "Cancel Service", "/Diagnostic/CancelService", "bi-x-circle"),
        new(MenuCodes.ApproveCancel, "Approve Cancel", "/Diagnostic/ApproveCancel", "bi-check2-square"),
        new(MenuCodes.OpdPackage, "OPD Package", "/Master/OpdPackage", "bi-box-seam"),
        new(MenuCodes.ParentGroup, "Parent Group", "/Master/ParentGroup", "bi-diagram-3"),
        new(MenuCodes.TestGroup, "TEST GROUP", "/Master/TestGroup", "bi-collection"),
        new(MenuCodes.TestItem, "Test Item", "/Master/TestItem", "bi-eyedropper"),
        new(MenuCodes.ReportParameter, "REPORT PARAMETER", "/Master/ReportParameter", "bi-sliders"),
        new(MenuCodes.Discount, "Discount", "/Master/DiscountSetup", "bi-percent"),
        new(MenuCodes.VacuMachine, "Vacu And Machine Group Mapping", "/Master/VacuMachineMapping", "bi-diagram-2"),
        new(MenuCodes.DeliveryTime, "Report Delivery Time Set", "/Master/ReportDeliveryTime", "bi-clock-history"),
        new(MenuCodes.LetterHead, "Letter Head Setup", "/Master/LetterHead", "bi-card-heading"),
        new(MenuCodes.Vacutainer, "Vacutainer", "/Master/Vacutainer", "bi-capsule"),
        new(MenuCodes.Machine, "Machine", "/Master/Machine", "bi-cpu"),
        new(MenuCodes.Specimen, "Specimen", "/Master/Specimen", "bi-droplet-half"),
        new(MenuCodes.Doctor, "Doctor", "/Master/Doctor", "bi-person-badge"),
        new(MenuCodes.Patient, "Patient", "/Master/Patient", "bi-people"),
        new(MenuCodes.DiagReports, "Reports", "/Reports", "bi-bar-chart"),
    ];

    private static readonly MenuSeed[] AdminMenus =
    [
        new(MenuCodes.Users, "Users", "/Master/User", "bi-person"),
        new(MenuCodes.Roles, "Roles", "/Master/Role", "bi-person-gear"),
        new(MenuCodes.MenuPermission, "Menu Permission", "/Admin/MenuPermission", "bi-shield-lock"),
        new(MenuCodes.Projects, "Projects", "/Master/Project", "bi-grid-3x3-gap"),
        new(MenuCodes.ProjectMenus, "Project Menus", "/Master/Menu", "bi-menu-button-wide"),
        new(MenuCodes.Tenants, "Tenants", "/Master/Tenant", "bi-building"),
        new(MenuCodes.ServiceUnits, "Service Units", "/Master/ServiceUnit", "bi-diagram-3"),
        new(MenuCodes.Departments, "Department", "/Master/Department", "bi-bank"),
        new(MenuCodes.PaymentMode, "Payment Mode", "/Master/PaymentMode", "bi-credit-card"),
        new(MenuCodes.PaymentChannel, "Payment Channel", "/Master/PaymentChannel", "bi-credit-card-2-front"),
        new(MenuCodes.Country, "Country/Nationality", "/Master/Country", "bi-globe"),
        new(MenuCodes.ReportPermission, "Report Permission Management", "/Admin/ReportPermission", "bi-bar-chart-line"),
        new(MenuCodes.ReportSpMapping, "Reports and Sp Mapping", "/Master/Report", "bi-bar-chart-steps"),
        new(MenuCodes.GlobalSettings, "APP GLOBAL SETTINGS", "/Master/AppGlobalSetting", "bi-gear"),
        new(MenuCodes.Printer, "Add/Edit Printer", "/Master/Printer", "bi-printer"),
        new(MenuCodes.Religion, "Religion", "/Master/Religion", "bi-flower1"),
    ];

    private async Task SeedProjectsAndMenusAsync()
    {
        var diag = await UpsertProjectAsync("DIAGNOSTIC", "Diagnostic", "bi-clipboard2-check", 1);
        var admin = await UpsertProjectAsync("ADMINISTRATIVE", "Administrative", "bi-clock", 2);

        var existing = await db.Menus.IgnoreQueryFilters().ToDictionaryAsync(m => m.Code ?? $"#{m.Id}");
        var added = new List<(Menu Menu, string[] Roles)>();
        void Upsert(Project p, MenuSeed[] seeds)
        {
            var n = 0; // position among menus without an explicit sort, so older menus keep their sort order
            foreach (var s in seeds)
            {
                if (s.Sort is null) n++;
                if (existing.ContainsKey(s.Code)) continue; // never overwrite edits made from the UI
                var menu = new Menu { Code = s.Code, Name = s.Name, Url = s.Url, Icon = s.Icon, ProjectId = p.Id, SortOrder = s.Sort ?? n * 10 };
                db.Menus.Add(menu);
                if (s.Roles is { Length: > 0 }) added.Add((menu, s.Roles));
            }
        }
        Upsert(diag, DiagnosticMenus);
        Upsert(admin, AdminMenus);
        await db.SaveChangesAsync();

        // Menus added to an existing database: give them to their default roles.
        foreach (var (menu, roleCodes) in added)
            foreach (var role in await db.Roles.Where(r => roleCodes.Contains(r.Code!)).ToListAsync())
                db.MenuPermissions.Add(new MenuPermission
                {
                    RoleId = role.Id, MenuId = menu.Id, CanView = true, CanAdd = true, CanEdit = true, CanPrint = true,
                });
        await db.SaveChangesAsync();
    }

    private async Task<Project> UpsertProjectAsync(string code, string name, string icon, int sort)
    {
        var p = await db.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Code == code);
        if (p != null) return p;
        p = new Project { Code = code, Name = name, Icon = icon, SortOrder = sort, Url = "/Home/Module/" + code };
        db.Projects.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    // ------------------------------------------------------------------ report mapping

    private record ParamSeed(string Name, string Label, string Type, bool Required = true, string? Default = null);
    private record ReportSeed(string Code, string Name, string Proc, string Template, ParamSeed[] Params, string Orientation = "Portrait");

    private static readonly ParamSeed[] DateRange = [new("@FromDate", "From Date", "date", true, "today"), new("@ToDate", "To Date", "date", true, "today")];

    private static readonly ReportSeed[] ReportSeeds =
    [
        new("LAB_REPORT", "Lab Report", "diag.usp_Rpt_LabReport", "LabReport",
            [new("@InvestigationId", "Invoice Id", "int"), new("@DetailIds", "Test Line Ids", "string", false)]),
        new("MONEY_RECEIPT", "Money Receipt", "diag.usp_Rpt_MoneyReceipt", "MoneyReceipt",
            [new("@InvestigationId", "Invoice Id", "int"), new("@PaymentId", "Payment Id", "int", false)]),
        new("PATHOLOGY_STICKER", "Pathology Sticker", "diag.usp_Rpt_PathologySticker", "Sticker",
            [new("@InvestigationId", "Invoice Id", "int", false), new("@SampleId", "Sample Id", "int", false)]),
        new("DAILY_COLLECTION", "Daily Collection Report", "diag.usp_Rpt_DailyCollection", "Tabular", DateRange, "Landscape"),
        new("TEST_WISE_INCOME", "Test Wise Income", "diag.usp_Rpt_TestWiseIncome", "Tabular",
            [..DateRange, new("@TestGroupId", "Test Group", "testgroup", false)]),
        new("DUE_LIST", "Due List", "diag.usp_Rpt_DueList", "Tabular", DateRange),
        new("CANCEL_REFUND", "Cancel & Refund Report", "diag.usp_Rpt_CancelRefund", "Tabular", DateRange, "Landscape"),
        new("DOCTOR_WISE_REFERRAL", "Doctor Wise Referral", "diag.usp_Rpt_DoctorWiseReferral", "Tabular", DateRange),
    ];

    private async Task SeedReportsAsync()
    {
        var project = await db.Projects.FirstAsync(p => p.Code == "DIAGNOSTIC");
        var codes = await db.Reports.IgnoreQueryFilters().Select(r => r.Code).ToListAsync();
        var sort = 10;
        foreach (var r in ReportSeeds)
        {
            sort += 10;
            if (codes.Contains(r.Code)) continue;
            db.Reports.Add(new Report
            {
                Code = r.Code, Name = r.Name, ProcedureName = r.Proc, Template = r.Template, Orientation = r.Orientation,
                ProjectId = project.Id, SortOrder = sort,
                Parameters = r.Params.Select((p, i) => new ReportSpParameter
                {
                    ParamName = p.Name, Label = p.Label, DataType = p.Type, IsRequired = p.Required, DefaultValue = p.Default, SortOrder = i + 1,
                }).ToList(),
            });
        }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ admin defaults

    private async Task SeedAdminAsync()
    {
        var tenant = new Tenant { Code = "HQ", Name = "Main Diagnostic Center", ShortName = "MDC", Address = "Dhaka, Bangladesh", Phone = "01700-000000" };
        db.Tenants.Add(tenant);

        var lab = new Department { Code = "LAB", Name = "Laboratory", Tenant = tenant, SortOrder = 1 };
        var rad = new Department { Code = "RAD", Name = "Radiology & Imaging", Tenant = tenant, SortOrder = 2 };
        var rec = new Department { Code = "REC", Name = "Reception & Billing", Tenant = tenant, SortOrder = 3 };
        db.Departments.AddRange(lab, rad, rec);
        db.ServiceUnits.AddRange(
            new ServiceUnit { Code = "SU-PATH", Name = "Pathology Lab", Department = lab },
            new ServiceUnit { Code = "SU-COLL", Name = "Sample Collection Room", Department = lab },
            new ServiceUnit { Code = "SU-XRAY", Name = "X-Ray Room", Department = rad },
            new ServiceUnit { Code = "SU-USG", Name = "Ultrasound Room", Department = rad },
            new ServiceUnit { Code = "SU-CASH", Name = "Cash Counter", Department = rec });

        var superAdmin = new Role { Code = "SUPER_ADMIN", Name = "Super Admin", IsSuperAdmin = true, Description = "Full access" };
        var reception = new Role { Code = "RECEPTION", Name = "Receptionist", Description = "Billing, sample collection, sticker" };
        var labTech = new Role { Code = "LAB_TECH", Name = "Lab Technician", Description = "Sample receive and result entry" };
        var pathologist = new Role { Code = "PATHOLOGIST", Name = "Pathologist", Description = "Result verification and report" };
        var accounts = new Role { Code = "ACCOUNTS", Name = "Accounts", Description = "Collection, cancel approval, reports" };
        db.Roles.AddRange(superAdmin, reception, labTech, pathologist, accounts);

        var admin = new User { UserName = "admin", FullName = "System Administrator", Tenant = tenant, Designation = "Administrator" };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");
        admin.UserRoles.Add(new UserRole { Role = superAdmin });
        db.Users.Add(admin);

        var cash = new PaymentMode { Code = "CASH", Name = "Cash", SortOrder = 1 };
        var card = new PaymentMode { Code = "CARD", Name = "Card", SortOrder = 2, RequiresReference = true };
        var mfs = new PaymentMode { Code = "MFS", Name = "Mobile Banking", SortOrder = 3, RequiresReference = true };
        var cheque = new PaymentMode { Code = "CHEQUE", Name = "Cheque", SortOrder = 4, RequiresReference = true };
        db.PaymentModes.AddRange(cash, card, mfs, cheque);
        db.PaymentChannels.AddRange(
            new PaymentChannel { Code = "VISA", Name = "Visa", PaymentMode = card, SortOrder = 1 },
            new PaymentChannel { Code = "MASTER", Name = "MasterCard", PaymentMode = card, SortOrder = 2 },
            new PaymentChannel { Code = "AMEX", Name = "American Express", PaymentMode = card, SortOrder = 3 },
            new PaymentChannel { Code = "BKASH", Name = "bKash", PaymentMode = mfs, SortOrder = 4 },
            new PaymentChannel { Code = "NAGAD", Name = "Nagad", PaymentMode = mfs, SortOrder = 5 },
            new PaymentChannel { Code = "ROCKET", Name = "Rocket", PaymentMode = mfs, SortOrder = 6 });

        db.Countries.AddRange(
            new Country { Code = "BD", Name = "Bangladesh", Nationality = "Bangladeshi", PhoneCode = "+880", SortOrder = 1 },
            new Country { Code = "IN", Name = "India", Nationality = "Indian", PhoneCode = "+91", SortOrder = 2 },
            new Country { Code = "NP", Name = "Nepal", Nationality = "Nepali", PhoneCode = "+977", SortOrder = 3 },
            new Country { Code = "LK", Name = "Sri Lanka", Nationality = "Sri Lankan", PhoneCode = "+94", SortOrder = 4 },
            new Country { Code = "OT", Name = "Other", Nationality = "Other", SortOrder = 99 });

        db.Religions.AddRange(
            new Religion { Code = "ISL", Name = "Islam", SortOrder = 1 },
            new Religion { Code = "HIN", Name = "Hinduism", SortOrder = 2 },
            new Religion { Code = "BUD", Name = "Buddhism", SortOrder = 3 },
            new Religion { Code = "CHR", Name = "Christianity", SortOrder = 4 },
            new Religion { Code = "OTH", Name = "Other", SortOrder = 5 });

        db.AppGlobalSettings.AddRange(
            new AppGlobalSetting { SettingGroup = "General", SettingKey = "App.Name", SettingValue = "HIS ERP", Description = "Application name shown in the header" },
            new AppGlobalSetting { SettingGroup = "General", SettingKey = "App.Currency", SettingValue = "BDT" },
            new AppGlobalSetting { SettingGroup = "Diagnostic", SettingKey = "Diag.DefaultDeliveryHours", SettingValue = "24", Description = "Used when no Report Delivery Time rule matches" },
            new AppGlobalSetting { SettingGroup = "Diagnostic", SettingKey = "Diag.PrintMoneyReceiptOnSave", SettingValue = "true" },
            new AppGlobalSetting { SettingGroup = "Diagnostic", SettingKey = "Diag.FinalizeRoleCodes", SettingValue = "PATHOLOGIST", Description = "Role code(s) allowed to finalize lab reports (comma separated). Super Admin is NOT included." },
            new AppGlobalSetting { SettingGroup = "Diagnostic", SettingKey = "Diag.PrintStickerOnCollect", SettingValue = "true" },
            new AppGlobalSetting { SettingGroup = "Report", SettingKey = "Report.ShowLetterHead", SettingValue = "true", Description = "false = print on pre-printed pad" });

        db.Printers.AddRange(
            new Printer { Code = "A4-01", Name = "Report Printer (A4)", PrinterType = PrinterType.A4, PaperSize = "A4", Tenant = tenant, IsDefault = true },
            new Printer { Code = "STK-01", Name = "Barcode Sticker Printer", PrinterType = PrinterType.Sticker, PaperSize = "50x25mm", Tenant = tenant },
            new Printer { Code = "POS-01", Name = "Money Receipt Printer", PrinterType = PrinterType.Pos, PaperSize = "80mm", Tenant = tenant });

        db.NumberSequences.AddRange(
            new NumberSequence { SequenceKey = "PATIENT", Prefix = "P", Padding = 6 },
            new NumberSequence { SequenceKey = "INVOICE", Prefix = "INV", Padding = 5 },
            new NumberSequence { SequenceKey = "RECEIPT", Prefix = "MR", Padding = 5 },
            new NumberSequence { SequenceKey = "SAMPLE", Prefix = "S", Padding = 5 });

        await db.SaveChangesAsync();

        // Default menu rights for the sample roles
        var menus = await db.Menus.ToDictionaryAsync(m => m.Code!);
        void Grant(Role role, bool full, params string[] codes)
        {
            foreach (var c in codes)
                if (menus.TryGetValue(c, out var m))
                    db.MenuPermissions.Add(new MenuPermission
                    {
                        RoleId = role.Id, MenuId = m.Id, CanView = true, CanPrint = true,
                        CanAdd = full, CanEdit = full, CanDelete = false,
                    });
        }
        Grant(reception, true, MenuCodes.Investigation, MenuCodes.InvoiceList, MenuCodes.SampleCollection, MenuCodes.Sticker,
            MenuCodes.CancelService, MenuCodes.Patient, MenuCodes.LabReport);
        Grant(reception, false, MenuCodes.DiagDashboard);
        Grant(labTech, true, MenuCodes.SampleCollection, MenuCodes.SampleAck, MenuCodes.Sticker, MenuCodes.ResultEntry);
        Grant(pathologist, true, MenuCodes.SampleAck, MenuCodes.ReportFinalize);
        Grant(reception, true, MenuCodes.ReportDelivery);
        Grant(accounts, true, MenuCodes.ReportDelivery);
        Grant(labTech, false, MenuCodes.InvoiceList);
        Grant(pathologist, true, MenuCodes.ResultEntry, MenuCodes.LabReport, MenuCodes.ReportParameter);
        Grant(pathologist, false, MenuCodes.TestItem, MenuCodes.InvoiceList);
        Grant(accounts, true, MenuCodes.InvoiceList, MenuCodes.ApproveCancel, MenuCodes.DiagReports);
        Grant(accounts, false, MenuCodes.DiagDashboard, MenuCodes.CancelService);
        await db.SaveChangesAsync();

        var reports = await db.Reports.ToListAsync();
        foreach (var r in reports)
        {
            db.ReportPermissions.Add(new ReportPermission { RoleId = accounts.Id, ReportId = r.Id, CanView = true, CanExport = true });
            if (r.Template != "Tabular")
                foreach (var role in new[] { reception, labTech, pathologist })
                    db.ReportPermissions.Add(new ReportPermission { RoleId = role.Id, ReportId = r.Id, CanView = true, CanExport = true });
        }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ diagnostic defaults

    private async Task SeedDiagnosticAsync()
    {
        var tenant = await db.Tenants.FirstAsync();
        var labDept = await db.Departments.FirstAsync(d => d.Code == "LAB");
        var radDept = await db.Departments.FirstAsync(d => d.Code == "RAD");

        var path = new ParentGroup { Code = "PATH", Name = "Pathology", SortOrder = 1 };
        var imaging = new ParentGroup { Code = "IMG", Name = "Radiology & Imaging", SortOrder = 2 };
        var cardio = new ParentGroup { Code = "CARD", Name = "Cardiology", SortOrder = 3 };

        var hema = new TestGroup { Code = "HEM", Name = "Hematology", ParentGroup = path, Department = labDept, SortOrder = 1, ReportHeading = "HAEMATOLOGY REPORT" };
        var bio = new TestGroup { Code = "BIO", Name = "Biochemistry", ParentGroup = path, Department = labDept, SortOrder = 2, ReportHeading = "BIOCHEMISTRY REPORT" };
        var sero = new TestGroup { Code = "SER", Name = "Serology & Immunology", ParentGroup = path, Department = labDept, SortOrder = 3, ReportHeading = "IMMUNOLOGY REPORT" };
        var clin = new TestGroup { Code = "CLP", Name = "Clinical Pathology", ParentGroup = path, Department = labDept, SortOrder = 4, ReportHeading = "CLINICAL PATHOLOGY REPORT" };
        var xray = new TestGroup { Code = "XRY", Name = "X-Ray", ParentGroup = imaging, Department = radDept, SortOrder = 5, ReportHeading = "RADIOLOGY REPORT" };
        var usg = new TestGroup { Code = "USG", Name = "Ultrasonography", ParentGroup = imaging, Department = radDept, SortOrder = 6, ReportHeading = "ULTRASONOGRAPHY REPORT" };
        var ecgGroup = new TestGroup { Code = "ECG", Name = "ECG", ParentGroup = cardio, SortOrder = 7, ReportHeading = "ECG REPORT" };

        var blood = new Specimen { Code = "BLD", Name = "Blood", SortOrder = 1 };
        var serum = new Specimen { Code = "SRM", Name = "Serum", SortOrder = 2 };
        var urine = new Specimen { Code = "URN", Name = "Urine", SortOrder = 3 };
        var stool = new Specimen { Code = "STL", Name = "Stool", SortOrder = 4 };

        var edta = new Vacutainer { Code = "EDTA", Name = "EDTA Tube", ColorName = "Purple", ColorHex = "#7B1FA2", SortOrder = 1 };
        var plain = new Vacutainer { Code = "PLAIN", Name = "Plain / Clot Activator", ColorName = "Red", ColorHex = "#D32F2F", SortOrder = 2 };
        var fluoride = new Vacutainer { Code = "FLU", Name = "Fluoride Tube", ColorName = "Grey", ColorHex = "#757575", SortOrder = 3 };
        var citrate = new Vacutainer { Code = "CIT", Name = "Citrate Tube", ColorName = "Blue", ColorHex = "#1976D2", SortOrder = 4 };
        var urineCont = new Vacutainer { Code = "URC", Name = "Urine Container", ColorName = "Yellow", ColorHex = "#FBC02D", SortOrder = 5 };

        var sysmex = new Machine { Code = "SYSMEX", Name = "Sysmex XN-550", Model = "XN-550", Manufacturer = "Sysmex", InterfaceCode = "XN550" };
        var cobas = new Machine { Code = "COBAS", Name = "Cobas c311", Model = "c311", Manufacturer = "Roche", InterfaceCode = "C311" };
        var maglumi = new Machine { Code = "MAGLUMI", Name = "Maglumi 800", Model = "800", Manufacturer = "Snibe", InterfaceCode = "MAG800" };
        var urineAn = new Machine { Code = "URIT", Name = "URIT-500B", Model = "500B", Manufacturer = "URIT" };

        db.AddRange(path, imaging, cardio, hema, bio, sero, clin, xray, usg, ecgGroup, blood, serum, urine, stool,
            edta, plain, fluoride, citrate, urineCont, sysmex, cobas, maglumi, urineAn);

        static ReportParameter P(int sort, string name, string? unit, string? range, decimal? low = null, decimal? high = null,
            string? heading = null, string? male = null, string? female = null, ParameterInputType type = ParameterInputType.Numeric,
            string? options = null, string? def = null) => new()
        {
            SortOrder = sort, Name = name, Unit = unit, NormalRange = range, LowValue = low, HighValue = high, GroupHeading = heading,
            NormalRangeMale = male, NormalRangeFemale = female, InputType = type, Options = options, DefaultValue = def,
        };

        var cbc = new TestItem
        {
            Code = "HEM001", Name = "Complete Blood Count (CBC)", ShortName = "CBC", TestGroup = hema, Specimen = blood, Price = 400,
            Parameters =
            [
                P(1, "Haemoglobin", "g/dL", "M: 13.0-17.0, F: 12.0-15.0", 12, 17, male: "13.0 - 17.0", female: "12.0 - 15.0"),
                P(2, "ESR (Westergren)", "mm/1st hr", "M: 0-10, F: 0-20", null, 20, male: "0 - 10", female: "0 - 20"),
                P(3, "Total WBC Count", "/cumm", "4,000 - 11,000", 4000, 11000),
                P(4, "Neutrophils", "%", "40 - 75", 40, 75, "DIFFERENTIAL COUNT"),
                P(5, "Lymphocytes", "%", "20 - 45", 20, 45, "DIFFERENTIAL COUNT"),
                P(6, "Monocytes", "%", "2 - 10", 2, 10, "DIFFERENTIAL COUNT"),
                P(7, "Eosinophils", "%", "1 - 6", 1, 6, "DIFFERENTIAL COUNT"),
                P(8, "Basophils", "%", "0 - 1", 0, 1, "DIFFERENTIAL COUNT"),
                P(9, "Total RBC Count", "million/cumm", "M: 4.5-5.5, F: 3.8-4.8", 3.8m, 5.5m, "RBC INDICES", "4.5 - 5.5", "3.8 - 4.8"),
                P(10, "HCT / PCV", "%", "M: 40-50, F: 36-46", 36, 50, "RBC INDICES", "40 - 50", "36 - 46"),
                P(11, "MCV", "fL", "83 - 101", 83, 101, "RBC INDICES"),
                P(12, "MCH", "pg", "27 - 32", 27, 32, "RBC INDICES"),
                P(13, "MCHC", "g/dL", "31.5 - 34.5", 31.5m, 34.5m, "RBC INDICES"),
                P(14, "Platelet Count", "/cumm", "150,000 - 450,000", 150000, 450000),
            ],
        };
        var fbs = new TestItem { Code = "BIO001", Name = "Blood Sugar (Fasting)", ShortName = "FBS", TestGroup = bio, Specimen = blood, Price = 150,
            Parameters = [P(1, "Blood Glucose (Fasting)", "mmol/L", "3.9 - 6.1", 3.9m, 6.1m)] };
        var rbs = new TestItem { Code = "BIO002", Name = "Blood Sugar (Random)", ShortName = "RBS", TestGroup = bio, Specimen = blood, Price = 150,
            Parameters = [P(1, "Blood Glucose (Random)", "mmol/L", "< 7.8", null, 7.8m)] };
        var creat = new TestItem { Code = "BIO003", Name = "Serum Creatinine", ShortName = "S.Creat", TestGroup = bio, Specimen = serum, Price = 350,
            Parameters = [P(1, "Serum Creatinine", "mg/dL", "M: 0.7-1.3, F: 0.6-1.1", 0.6m, 1.3m, male: "0.7 - 1.3", female: "0.6 - 1.1")] };
        var sgpt = new TestItem { Code = "BIO004", Name = "SGPT (ALT)", ShortName = "SGPT", TestGroup = bio, Specimen = serum, Price = 350,
            Parameters = [P(1, "SGPT (ALT)", "U/L", "Up to 41", null, 41)] };
        var lipid = new TestItem
        {
            Code = "BIO005", Name = "Lipid Profile (Fasting)", ShortName = "Lipid", TestGroup = bio, Specimen = serum, Price = 1200,
            Parameters =
            [
                P(1, "Total Cholesterol", "mg/dL", "< 200", null, 200),
                P(2, "Triglyceride", "mg/dL", "< 150", null, 150),
                P(3, "HDL Cholesterol", "mg/dL", "> 40", 40, null),
                P(4, "LDL Cholesterol", "mg/dL", "< 100", null, 100),
            ],
        };
        var tsh = new TestItem { Code = "SER001", Name = "TSH", ShortName = "TSH", TestGroup = sero, Specimen = serum, Price = 800,
            Parameters = [P(1, "TSH", "µIU/mL", "0.27 - 4.20", 0.27m, 4.2m, def: null)] };
        var hbsag = new TestItem { Code = "SER002", Name = "HBsAg (Screening)", ShortName = "HBsAg", TestGroup = sero, Specimen = serum, Price = 500,
            Parameters = [P(1, "HBsAg", null, "Negative", type: ParameterInputType.Option, options: "Negative,Positive", def: "Negative")] };
        var ure = new TestItem
        {
            Code = "CLP001", Name = "Urine Routine Examination", ShortName = "Urine R/E", TestGroup = clin, Specimen = urine, Price = 250,
            Parameters =
            [
                P(1, "Colour", null, "Straw", heading: "PHYSICAL EXAMINATION", type: ParameterInputType.Option, options: "Straw,Pale yellow,Yellow,Amber,Red", def: "Straw"),
                P(2, "Appearance", null, "Clear", heading: "PHYSICAL EXAMINATION", type: ParameterInputType.Option, options: "Clear,Slightly hazy,Turbid", def: "Clear"),
                P(3, "Albumin", null, "Nil", heading: "CHEMICAL EXAMINATION", type: ParameterInputType.Option, options: "Nil,Trace,+,++,+++", def: "Nil"),
                P(4, "Sugar", null, "Nil", heading: "CHEMICAL EXAMINATION", type: ParameterInputType.Option, options: "Nil,Trace,+,++,+++", def: "Nil"),
                P(5, "Pus Cells", "/HPF", "0 - 5", heading: "MICROSCOPIC EXAMINATION", type: ParameterInputType.Text, def: "2-4"),
                P(6, "RBC", "/HPF", "Nil", heading: "MICROSCOPIC EXAMINATION", type: ParameterInputType.Text, def: "Nil"),
                P(7, "Epithelial Cells", "/HPF", "Few", heading: "MICROSCOPIC EXAMINATION", type: ParameterInputType.Text, def: "Few"),
            ],
        };
        var cxr = new TestItem
        {
            Code = "XRY001", Name = "X-Ray Chest P/A View", ShortName = "CXR P/A", TestGroup = xray, Price = 500, ResultType = TestResultType.Radiology,
            RequiresSample = false,
            DefaultReportText = "Both lung fields are clear.\nNo active parenchymal lesion is seen.\nBoth domes of diaphragm and CP angles are normal.\nCardiac shadow is within normal limit.\n\nIMPRESSION: Normal chest skiagram.",
        };
        var usgWa = new TestItem
        {
            Code = "USG001", Name = "USG of Whole Abdomen", ShortName = "USG W/A", TestGroup = usg, Price = 1800, ResultType = TestResultType.Radiology,
            RequiresSample = false,
            DefaultReportText = "LIVER: Normal in size and echotexture. No focal lesion.\nGALL BLADDER: Normal. No calculus.\nPANCREAS: Normal.\nSPLEEN: Normal.\nKIDNEYS: Both kidneys are normal in size, shape and position.\nURINARY BLADDER: Normal.\n\nIMPRESSION: Normal study.",
        };
        var ecg = new TestItem { Code = "ECG001", Name = "ECG (12 Lead)", ShortName = "ECG", TestGroup = ecgGroup, Price = 400, ResultType = TestResultType.Text,
            RequiresSample = false, DefaultReportText = "Rhythm: Sinus\nRate: 78 bpm\nAxis: Normal\n\nIMPRESSION: Normal ECG." };

        db.TestItems.AddRange(cbc, fbs, rbs, creat, sgpt, lipid, tsh, hbsag, ure, cxr, usgWa, ecg);

        db.VacuMachineMappings.AddRange(
            new VacuMachineMapping { TestGroup = hema, Vacutainer = edta, Machine = sysmex, SampleVolume = "2 mL" },
            new VacuMachineMapping { TestGroup = bio, Vacutainer = plain, Machine = cobas, SampleVolume = "3 mL" },
            new VacuMachineMapping { TestGroup = bio, TestItem = fbs, Vacutainer = fluoride, Machine = cobas, SampleVolume = "2 mL" },
            new VacuMachineMapping { TestGroup = bio, TestItem = rbs, Vacutainer = fluoride, Machine = cobas, SampleVolume = "2 mL" },
            new VacuMachineMapping { TestGroup = sero, Vacutainer = plain, Machine = maglumi, SampleVolume = "3 mL" },
            new VacuMachineMapping { TestGroup = clin, Vacutainer = urineCont, Machine = urineAn, SampleVolume = "20 mL" });

        db.ReportDeliveryTimes.AddRange(
            new ReportDeliveryTime { TestGroup = hema, DeliveryHours = 4, Remarks = "Same day" },
            new ReportDeliveryTime { TestGroup = bio, DeliveryHours = 6 },
            new ReportDeliveryTime { TestGroup = sero, DeliveryHours = 24, DeliveryAt = new TimeOnly(18, 0), Remarks = "Next day 6 PM" },
            new ReportDeliveryTime { TestGroup = clin, DeliveryHours = 3 },
            new ReportDeliveryTime { TestGroup = xray, DeliveryHours = 2 },
            new ReportDeliveryTime { TestGroup = usg, DeliveryHours = 1 },
            new ReportDeliveryTime { TestGroup = ecgGroup, DeliveryHours = 1 },
            new ReportDeliveryTime { TestGroup = sero, TestItem = tsh, DeliveryHours = 48, Remarks = "Batch run" });

        db.Doctors.AddRange(
            new Doctor { Code = "D001", Name = "Dr. Rahim Uddin", Degree = "MBBS, FCPS (Medicine)", Designation = "Consultant", Phone = "01711-000001", IsReferrer = true, CommissionPercent = 10 },
            new Doctor { Code = "D002", Name = "Dr. Nasrin Akter", Degree = "MBBS, DGO", Designation = "Gynaecologist", Phone = "01711-000002", IsReferrer = true, CommissionPercent = 10 },
            new Doctor { Code = "D101", Name = "Dr. Kamrul Hasan", Degree = "MBBS, M.Phil (Pathology)", Designation = "Consultant Pathologist", IsReferrer = false, IsReportSigner = true },
            new Doctor { Code = "D102", Name = "Dr. Farhana Islam", Degree = "MBBS, DMU", Designation = "Consultant Sonologist", IsReferrer = false, IsReportSigner = true });

        db.OpdPackages.Add(new OpdPackage
        {
            Code = "PKG001", Name = "Basic Health Check-up", PackagePrice = 1000, Description = "CBC, FBS, S.Creatinine, Urine R/E",
            Items =
            [
                new OpdPackageItem { TestItem = cbc, ItemPrice = 350 },
                new OpdPackageItem { TestItem = fbs, ItemPrice = 130 },
                new OpdPackageItem { TestItem = creat, ItemPrice = 300 },
                new OpdPackageItem { TestItem = ure, ItemPrice = 220 },
            ],
        });

        db.DiscountSetups.AddRange(
            new DiscountSetup { Code = "STAFF", Name = "Staff Discount 20%", DiscountType = DiscountType.Percent, Value = 20 },
            new DiscountSetup { Code = "SENIOR", Name = "Senior Citizen 10%", DiscountType = DiscountType.Percent, Value = 10 },
            new DiscountSetup { Code = "PATH15", Name = "Pathology Campaign 15%", DiscountType = DiscountType.Percent, Value = 15, TestGroup = hema });

        db.LetterHeads.Add(new LetterHead
        {
            Code = "LH-MAIN", Name = "Main Letter Head", Tenant = tenant, IsDefault = true,
            Title = tenant.Name, SubTitle = "Diagnostic & Imaging Services", Address = tenant.Address, Phone = tenant.Phone,
            FooterText = "This report is electronically verified. Please correlate clinically.",
        });

        await db.SaveChangesAsync();
    }
}
