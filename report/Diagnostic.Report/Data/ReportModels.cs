namespace Diagnostic.Report.Data;

// Row shapes returned by the diag.usp_Rpt_* stored procedures (Dapper maps columns by name).

public class ReportDefinition
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ProcedureName { get; set; } = "";
    public string Template { get; set; } = "Tabular";
    public string? Orientation { get; set; }
    public List<ReportParam> Parameters { get; set; } = [];
}

public class ReportParam
{
    public int ReportId { get; set; }
    public string ParamName { get; set; } = "";
    public string Label { get; set; } = "";
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
}

public class LetterHeadRow
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? SubTitle { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public string? FooterText { get; set; }
    public int PrePrintedTopMarginMm { get; set; } = 45;
}

public class InvoiceHeaderRow
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public string PatientCode { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? Gender { get; set; }
    public string? PatientAge { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ReferDoctor { get; set; }
    public string? PackageName { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string? Status { get; set; }
    public string? PreparedBy { get; set; }
    public int TenantId { get; set; }
    public string? ReceiptNo { get; set; }
    public decimal? ReceiptAmount { get; set; }
    public string? ReceiptType { get; set; }
    public DateTime? ReceiptDate { get; set; }
    // lab report only
    public string? SampleNo { get; set; }
    public DateTime? CollectedAt { get; set; }
}

public class InvoiceTestRow
{
    public long Sl { get; set; }
    public string? TestCode { get; set; }
    public string TestName { get; set; } = "";
    public string? GroupName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? DeliveryDateTime { get; set; }
}

public class InvoicePaymentRow
{
    public string ReceiptNo { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public string PaymentType { get; set; } = "";
    public string PaymentMode { get; set; } = "";
    public string? PaymentChannel { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionRef { get; set; }
}

public class LabTestRow
{
    public int DetailId { get; set; }
    public string TestName { get; set; } = "";
    public string ResultType { get; set; } = "Parameter";
    public string? GroupName { get; set; }
    public string? ReportHeading { get; set; }
    public string? ParentGroupName { get; set; }
    public string? ReportText { get; set; }
    public string? Comments { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? EnteredAt { get; set; }
    public string? SignDoctorName { get; set; }
    public string? SignDoctorDegree { get; set; }
    public string? SignDoctorDesignation { get; set; }
    public string? EnteredBy { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Specimen { get; set; }
    public string? TestStatus { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }
}

public class LabResultRow
{
    public int DetailId { get; set; }
    public string ParameterName { get; set; } = "";
    public string? GroupHeading { get; set; }
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? NormalRange { get; set; }
    public string? Flag { get; set; }
    public string? Method { get; set; }
    public string? Remarks { get; set; }
    public int SortOrder { get; set; }
}

public class StickerRow
{
    public int Id { get; set; }
    public string SampleNo { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public string PatientCode { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? Gender { get; set; }
    public string? PatientAge { get; set; }
    public DateTime CollectedAt { get; set; }
    public string? Vacutainer { get; set; }
    public string? ColorHex { get; set; }
    public string? Specimen { get; set; }
    public string? Tests { get; set; }
}

public record MoneyReceiptData(LetterHeadRow? LetterHead, InvoiceHeaderRow Header, List<InvoiceTestRow> Tests, List<InvoicePaymentRow> Payments);

public record LabReportData(LetterHeadRow? LetterHead, InvoiceHeaderRow Header, List<LabTestRow> Tests, List<LabResultRow> Results);

public record TabularData(string Title, string Subtitle, List<string> Columns, List<object?[]> Rows);
