using System.Text.Json.Serialization;
using Diagnostic.Api.Common;
using Diagnostic.Api.Entities.Admin;

namespace Diagnostic.Api.Entities.Diag;

// ======================= Test catalogue =======================

/// <summary>Top level: Pathology, Radiology &amp; Imaging, Cardiology ...</summary>
public class ParentGroup : MasterEntity { }

/// <summary>Hematology, Biochemistry, Serology, X-Ray, USG ...</summary>
public class TestGroup : MasterEntity
{
    public int ParentGroupId { get; set; }
    public ParentGroup? ParentGroup { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    /// <summary>Heading printed on the lab report (e.g. "HAEMATOLOGY REPORT").</summary>
    public string? ReportHeading { get; set; }
}

public class Specimen : MasterEntity { }

/// <summary>Sample tube: EDTA (purple), Plain (red), Citrate (blue) ...</summary>
public class Vacutainer : MasterEntity
{
    public string? ColorName { get; set; }
    public string? ColorHex { get; set; }
}

/// <summary>Lab analyser that runs the test.</summary>
public class Machine : MasterEntity
{
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    /// <summary>Code used by the LIS interface to push results.</summary>
    public string? InterfaceCode { get; set; }
}

public class TestItem : MasterEntity
{
    public int TestGroupId { get; set; }
    public TestGroup? TestGroup { get; set; }
    public string? ShortName { get; set; }
    public decimal Price { get; set; }
    public decimal VatPercent { get; set; }
    public bool IsDiscountAllowed { get; set; } = true;
    public decimal MaxDiscountPercent { get; set; } = 100;
    public int? SpecimenId { get; set; }
    public Specimen? Specimen { get; set; }
    public TestResultType ResultType { get; set; } = TestResultType.Parameter;
    /// <summary>Template text for Text / Radiology type reports.</summary>
    public string? DefaultReportText { get; set; }
    public bool IsOutsource { get; set; }
    public bool RequiresSample { get; set; } = true;
    public List<ReportParameter> Parameters { get; set; } = [];
}

/// <summary>A line on the lab report of a test item (Hemoglobin, ESR, WBC ...), with its unit and reference range.</summary>
public class ReportParameter : BaseEntity
{
    public int TestItemId { get; set; }
    [JsonIgnore] public TestItem? TestItem { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Sub-heading printed above the parameter group (e.g. "DIFFERENTIAL COUNT").</summary>
    public string? GroupHeading { get; set; }
    public string? Unit { get; set; }
    public string? NormalRange { get; set; }
    public string? NormalRangeMale { get; set; }
    public string? NormalRangeFemale { get; set; }
    public decimal? LowValue { get; set; }
    public decimal? HighValue { get; set; }
    public string? Method { get; set; }
    public ParameterInputType InputType { get; set; } = ParameterInputType.Numeric;
    /// <summary>Comma separated options when <see cref="InputType"/> = Option (e.g. "Positive,Negative").</summary>
    public string? Options { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Vacutainer &amp; machine used for a test group (optionally overridden for a single test item).</summary>
public class VacuMachineMapping : BaseEntity
{
    public int TestGroupId { get; set; }
    public TestGroup? TestGroup { get; set; }
    public int? TestItemId { get; set; }
    public TestItem? TestItem { get; set; }
    public int VacutainerId { get; set; }
    public Vacutainer? Vacutainer { get; set; }
    public int? MachineId { get; set; }
    public Machine? Machine { get; set; }
    public string? SampleVolume { get; set; }
}

/// <summary>How long after billing a report is ready. Item-level row wins over group-level row.</summary>
public class ReportDeliveryTime : BaseEntity
{
    public int? TestGroupId { get; set; }
    public TestGroup? TestGroup { get; set; }
    public int? TestItemId { get; set; }
    public TestItem? TestItem { get; set; }
    public int DeliveryHours { get; set; } = 24;
    /// <summary>Optional fixed delivery clock time (e.g. 18:00) on the computed day.</summary>
    public TimeOnly? DeliveryAt { get; set; }
    public string? Remarks { get; set; }
}

public class OpdPackage : MasterEntity
{
    public decimal PackagePrice { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? Description { get; set; }
    public List<OpdPackageItem> Items { get; set; } = [];
}

public class OpdPackageItem : BaseEntity
{
    public int OpdPackageId { get; set; }
    [JsonIgnore] public OpdPackage? OpdPackage { get; set; }
    public int TestItemId { get; set; }
    public TestItem? TestItem { get; set; }
    /// <summary>Share of the package price allocated to this test (used for income reports).</summary>
    public decimal ItemPrice { get; set; }
}

public class LetterHead : MasterEntity
{
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string? Title { get; set; }
    public string? SubTitle { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public string? FooterText { get; set; }
    /// <summary>When printing on pre-printed pad, header is left blank with this margin.</summary>
    public int PrePrintedTopMarginMm { get; set; } = 45;
    public bool IsDefault { get; set; }
}

public class Doctor : MasterEntity
{
    public string? Degree { get; set; }
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public bool IsReferrer { get; set; } = true;
    public bool IsConsultant { get; set; }
    /// <summary>Doctor who signs lab reports (pathologist / radiologist).</summary>
    public bool IsReportSigner { get; set; }
    public decimal CommissionPercent { get; set; }
}

/// <summary>Media / agent who refers patients and earns commission on their tests.</summary>
public class Media : MasterEntity
{
    public string? Phone { get; set; }
    public string? Organization { get; set; }
    public string? Address { get; set; }
    /// <summary>Used when no Media Commission Setup row matches a test.</summary>
    public decimal DefaultCommissionPercent { get; set; }
}

/// <summary>
/// Media commission rule. Matching order for a billed test: test item rule → test group rule → media default %.
/// A rule with no media applies to every media (a media-specific rule wins over it).
/// </summary>
public class MediaCommissionSetup : BaseEntity
{
    public int? MediaId { get; set; }
    public Media? Media { get; set; }
    public int? TestGroupId { get; set; }
    public TestGroup? TestGroup { get; set; }
    public int? TestItemId { get; set; }
    public TestItem? TestItem { get; set; }
    public DiscountType CommissionType { get; set; } = DiscountType.Percent;
    public decimal Value { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Discount scheme. Limit it to one test (test wise) or one test group (group wise); empty = all tests.</summary>
public class DiscountSetup : MasterEntity
{
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    public decimal Value { get; set; }
    public int? TestGroupId { get; set; }
    public TestGroup? TestGroup { get; set; }
    public int? TestItemId { get; set; }
    public TestItem? TestItem { get; set; }
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

// ======================= Transactions =======================

public class Patient : BaseEntity
{
    public string PatientCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public int? ReligionId { get; set; }
    public Religion? Religion { get; set; }
    public int? CountryId { get; set; }
    public Country? Country { get; set; }
}

/// <summary>Investigation bill (invoice) – header.</summary>
public class Investigation : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    /// <summary>Age snapshot at billing time, printed on reports.</summary>
    public string? PatientAge { get; set; }
    public int? ReferDoctorId { get; set; }
    public Doctor? ReferDoctor { get; set; }
    public int? MediaId { get; set; }
    public Media? Media { get; set; }
    /// <summary>Sum of line commissions of non-cancelled tests.</summary>
    public decimal MediaCommission { get; set; }
    public int? OpdPackageId { get; set; }
    public OpdPackage? OpdPackage { get; set; }
    public int? DiscountSetupId { get; set; }
    public DiscountSetup? DiscountSetup { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal DueAmount { get; set; }
    public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;
    public string? Remarks { get; set; }
    public List<InvestigationDetail> Details { get; set; } = [];
    public List<InvestigationPayment> Payments { get; set; } = [];
}

public class InvestigationDetail : BaseEntity
{
    public int InvestigationId { get; set; }
    [JsonIgnore] public Investigation? Investigation { get; set; }
    public int TestItemId { get; set; }
    public TestItem? TestItem { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal NetAmount { get; set; }
    /// <summary>Media commission earned on this test (calculated at billing from Media Commission Setup).</summary>
    public decimal MediaCommission { get; set; }
    public TestStatus Status { get; set; } = TestStatus.Ordered;
    public DateTime? DeliveryDateTime { get; set; }
    public int? SampleCollectionId { get; set; }
    public SampleCollection? SampleCollection { get; set; }
}

/// <summary>Money receipt: payment, due collection or refund against an invoice.</summary>
public class InvestigationPayment : BaseEntity
{
    public int InvestigationId { get; set; }
    [JsonIgnore] public Investigation? Investigation { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Payment;
    public int PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public int? PaymentChannelId { get; set; }
    public PaymentChannel? PaymentChannel { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionRef { get; set; }
}

/// <summary>"Cancel Service" creates a request; "Approve Cancel" approves or rejects it.</summary>
public class CancelRequest : BaseEntity
{
    public int InvestigationId { get; set; }
    public Investigation? Investigation { get; set; }
    /// <summary>Null = cancel the whole invoice.</summary>
    public int? InvestigationDetailId { get; set; }
    public InvestigationDetail? InvestigationDetail { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public CancelStatus Status { get; set; } = CancelStatus.Pending;
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalRemarks { get; set; }
}

/// <summary>One tube collected from the patient; its <see cref="SampleNo"/> is the barcode on the sticker.</summary>
public class SampleCollection : BaseEntity
{
    public string SampleNo { get; set; } = string.Empty;
    public int InvestigationId { get; set; }
    public Investigation? Investigation { get; set; }
    public int? VacutainerId { get; set; }
    public Vacutainer? Vacutainer { get; set; }
    public int? SpecimenId { get; set; }
    public Specimen? Specimen { get; set; }
    public SampleStatus Status { get; set; } = SampleStatus.Collected;
    public DateTime CollectedAt { get; set; }
    public int? CollectedBy { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public int? DispatchedBy { get; set; }
    /// <summary>Acknowledged (received) by the lab.</summary>
    public DateTime? ReceivedAt { get; set; }
    public int? ReceivedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedBy { get; set; }
    public string? RejectReason { get; set; }
    /// <summary>When this tube is a repeat collection, the rejected tube it replaces.</summary>
    public int? RepeatOfSampleId { get; set; }
    [JsonIgnore] public SampleCollection? RepeatOfSample { get; set; }
    public List<InvestigationDetail> Details { get; set; } = [];
}

/// <summary>Result header of one billed test.</summary>
public class LabReport : BaseEntity
{
    public int InvestigationDetailId { get; set; }
    public InvestigationDetail? InvestigationDetail { get; set; }
    /// <summary>Free text / radiology findings.</summary>
    public string? ReportText { get; set; }
    public string? Comments { get; set; }
    public int? EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }
    public bool IsVerified { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int? SignDoctorId { get; set; }
    public Doctor? SignDoctor { get; set; }
    /// <summary>Final approval by a pathologist (Lab Report Finalization). Only finalized reports can be delivered.</summary>
    public bool IsFinalized { get; set; }
    public int? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }
    /// <summary>Why the pathologist sent the result back to the lab (shown in Result Entry).</summary>
    public string? ReturnReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveredTo { get; set; }
    public int PrintCount { get; set; }
    public List<LabResult> Results { get; set; } = [];
}

public class LabResult : BaseEntity
{
    public int LabReportId { get; set; }
    [JsonIgnore] public LabReport? LabReport { get; set; }
    public int ReportParameterId { get; set; }
    public ReportParameter? ReportParameter { get; set; }
    public string? ResultValue { get; set; }
    public ResultFlag Flag { get; set; } = ResultFlag.Normal;
    public string? Remarks { get; set; }
}
