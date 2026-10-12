namespace Diagnostic.Api.Security;

/// <summary>Codes of admin.Menus rows. Keep in sync with <see cref="Data.DbSeeder"/>.</summary>
public static class MenuCodes
{
    // Administrative
    public const string Users = "ADM_USERS";
    public const string Roles = "ADM_ROLES";
    public const string MenuPermission = "ADM_MENU_PERMISSION";
    public const string Projects = "ADM_PROJECTS";
    public const string ProjectMenus = "ADM_PROJECT_MENUS";
    public const string Tenants = "ADM_TENANTS";
    public const string ServiceUnits = "ADM_SERVICE_UNITS";
    public const string Departments = "ADM_DEPARTMENT";
    public const string PaymentMode = "ADM_PAYMENT_MODE";
    public const string PaymentChannel = "ADM_PAYMENT_CHANNEL";
    public const string Country = "ADM_COUNTRY";
    public const string ReportPermission = "ADM_REPORT_PERMISSION";
    public const string ReportSpMapping = "ADM_REPORT_SP_MAPPING";
    public const string GlobalSettings = "ADM_GLOBAL_SETTINGS";
    public const string Printer = "ADM_PRINTER";
    public const string Religion = "ADM_RELIGION";

    // Diagnostic
    public const string DiagDashboard = "DIAG_DASHBOARD";
    public const string Investigation = "DIAG_INVESTIGATION";
    public const string InvoiceList = "DIAG_INVOICE_LIST";
    public const string SampleCollection = "DIAG_SAMPLE_COLLECTION";
    public const string SampleAck = "DIAG_SAMPLE_ACK";
    public const string ResultEntry = "DIAG_RESULT_ENTRY";
    public const string LabReport = "DIAG_LAB_REPORT";
    public const string ReportFinalize = "DIAG_REPORT_FINALIZE";
    public const string ReportDelivery = "DIAG_REPORT_DELIVERY";
    public const string Sticker = "DIAG_STICKER";
    public const string CancelService = "DIAG_CANCEL_SERVICE";
    public const string ApproveCancel = "DIAG_APPROVE_CANCEL";
    public const string OpdPackage = "DIAG_OPD_PACKAGE";
    public const string ParentGroup = "DIAG_PARENT_GROUP";
    public const string TestGroup = "DIAG_TEST_GROUP";
    public const string TestItem = "DIAG_TEST_ITEM";
    public const string ReportParameter = "DIAG_REPORT_PARAMETER";
    public const string Discount = "DIAG_DISCOUNT";
    public const string Media = "DIAG_MEDIA";
    public const string MediaCommission = "DIAG_MEDIA_COMMISSION";
    public const string VacuMachine = "DIAG_VACU_MACHINE";
    public const string DeliveryTime = "DIAG_DELIVERY_TIME";
    public const string LetterHead = "DIAG_LETTER_HEAD";
    public const string Vacutainer = "DIAG_VACUTAINER";
    public const string Machine = "DIAG_MACHINE";
    public const string Specimen = "DIAG_SPECIMEN";
    public const string Doctor = "DIAG_DOCTOR";
    public const string Patient = "DIAG_PATIENT";
    public const string DiagReports = "DIAG_REPORTS";
}
