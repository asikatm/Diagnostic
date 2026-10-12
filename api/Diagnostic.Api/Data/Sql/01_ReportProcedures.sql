-- =====================================================================================
-- Stored procedures used by the report server (see admin.Reports = "Reports and Sp Mapping").
-- Executed automatically by the API at start-up (CREATE OR ALTER – safe to run many times).
-- =====================================================================================

-- Letter head / company header used on every printed document
CREATE OR ALTER PROCEDURE diag.usp_Rpt_LetterHead
    @LetterHeadId INT = NULL,
    @TenantId     INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        lh.Id, lh.Name, ISNULL(lh.Title, t.Name) AS Title, lh.SubTitle,
        ISNULL(lh.Address, t.Address) AS Address, ISNULL(lh.Phone, t.Phone) AS Phone, ISNULL(lh.Email, t.Email) AS Email,
        ISNULL(lh.LogoPath, t.LogoPath) AS LogoPath, lh.FooterText, lh.PrePrintedTopMarginMm
    FROM diag.LetterHeads lh
    LEFT JOIN admin.Tenants t ON t.Id = lh.TenantId
    WHERE lh.IsDeleted = 0 AND lh.IsActive = 1
      AND (@LetterHeadId IS NULL OR lh.Id = @LetterHeadId)
      AND (@TenantId IS NULL OR lh.TenantId = @TenantId OR lh.TenantId IS NULL)
    ORDER BY CASE WHEN lh.Id = @LetterHeadId THEN 0 ELSE 1 END, lh.IsDefault DESC, lh.Id;
END
GO

-- Money receipt / invoice copy. Result sets: 1 header, 2 tests, 3 payments
CREATE OR ALTER PROCEDURE diag.usp_Rpt_MoneyReceipt
    @InvestigationId INT,
    @PaymentId       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT i.Id, i.InvoiceNo, i.InvoiceDate, p.PatientCode, p.Name AS PatientName, p.Gender, i.PatientAge, p.Phone, p.Address,
           d.Name AS ReferDoctor, pk.Name AS PackageName,
           i.GrossAmount, i.DiscountAmount, i.VatAmount, i.NetAmount, i.PaidAmount, i.RefundAmount, i.DueAmount, i.Status,
           u.FullName AS PreparedBy, i.TenantId,
           rp.ReceiptNo, rp.Amount AS ReceiptAmount, rp.PaymentType AS ReceiptType, rp.PaymentDate AS ReceiptDate
    FROM diag.Investigations i
    JOIN diag.Patients p ON p.Id = i.PatientId
    LEFT JOIN diag.Doctors d ON d.Id = i.ReferDoctorId
    LEFT JOIN diag.OpdPackages pk ON pk.Id = i.OpdPackageId
    LEFT JOIN admin.Users u ON u.Id = i.CreatedBy
    LEFT JOIN diag.InvestigationPayments rp ON rp.Id = @PaymentId AND rp.InvestigationId = i.Id
    WHERE i.Id = @InvestigationId;

    SELECT ROW_NUMBER() OVER (ORDER BY dt.Id) AS Sl, ti.Code AS TestCode, ti.Name AS TestName, tg.Name AS GroupName,
           dt.UnitPrice, dt.DiscountAmount, dt.VatAmount, dt.NetAmount, dt.Status, dt.DeliveryDateTime
    FROM diag.InvestigationDetails dt
    JOIN diag.TestItems ti ON ti.Id = dt.TestItemId
    JOIN diag.TestGroups tg ON tg.Id = ti.TestGroupId
    WHERE dt.InvestigationId = @InvestigationId AND dt.IsDeleted = 0
    ORDER BY dt.Id;

    SELECT py.ReceiptNo, py.PaymentDate, py.PaymentType, pm.Name AS PaymentMode, pc.Name AS PaymentChannel, py.Amount, py.TransactionRef
    FROM diag.InvestigationPayments py
    JOIN admin.PaymentModes pm ON pm.Id = py.PaymentModeId
    LEFT JOIN admin.PaymentChannels pc ON pc.Id = py.PaymentChannelId
    WHERE py.InvestigationId = @InvestigationId AND py.IsDeleted = 0
    ORDER BY py.Id;
END
GO

-- Lab report. Result sets: 1 header, 2 tests, 3 parameter results.
-- Only tests finalized by the pathologist are printed; @Preview = 1 also includes verified (not yet finalized) tests.
CREATE OR ALTER PROCEDURE diag.usp_Rpt_LabReport
    @InvestigationId INT,
    @DetailIds       NVARCHAR(MAX) = NULL,  -- comma separated InvestigationDetails.Id, NULL = all printable tests
    @Preview         BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ids TABLE (Id INT PRIMARY KEY);
    IF @DetailIds IS NOT NULL AND LEN(@DetailIds) > 0
        INSERT INTO @ids SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(@DetailIds, ',') WHERE TRY_CAST(value AS INT) IS NOT NULL;

    SELECT i.Id, i.InvoiceNo, i.InvoiceDate, p.PatientCode, p.Name AS PatientName, p.Gender, i.PatientAge, p.Phone,
           d.Name AS ReferDoctor, i.TenantId,
           (SELECT STRING_AGG(sc.SampleNo, ', ') FROM diag.SampleCollections sc WHERE sc.InvestigationId = i.Id AND sc.Status <> 'Rejected') AS SampleNo,
           (SELECT MIN(sc.CollectedAt) FROM diag.SampleCollections sc WHERE sc.InvestigationId = i.Id AND sc.Status <> 'Rejected') AS CollectedAt
    FROM diag.Investigations i
    JOIN diag.Patients p ON p.Id = i.PatientId
    LEFT JOIN diag.Doctors d ON d.Id = i.ReferDoctorId
    WHERE i.Id = @InvestigationId;

    SELECT dt.Id AS DetailId, ti.Name AS TestName, ti.ResultType, tg.Name AS GroupName,
           ISNULL(tg.ReportHeading, UPPER(tg.Name) + ' REPORT') AS ReportHeading, pg.Name AS ParentGroupName,
           lr.ReportText, lr.Comments, lr.VerifiedAt, lr.EnteredAt,
           sd.Name AS SignDoctorName, sd.Degree AS SignDoctorDegree, sd.Designation AS SignDoctorDesignation,
           eu.FullName AS EnteredBy, vu.FullName AS VerifiedBy, sp.Name AS Specimen,
           dt.Status AS TestStatus, lr.FinalizedAt, fu.FullName AS FinalizedBy
    FROM diag.InvestigationDetails dt
    JOIN diag.TestItems ti ON ti.Id = dt.TestItemId
    JOIN diag.TestGroups tg ON tg.Id = ti.TestGroupId
    JOIN diag.ParentGroups pg ON pg.Id = tg.ParentGroupId
    JOIN diag.LabReports lr ON lr.InvestigationDetailId = dt.Id AND lr.IsDeleted = 0
    LEFT JOIN diag.Doctors sd ON sd.Id = lr.SignDoctorId
    LEFT JOIN admin.Users eu ON eu.Id = lr.EnteredBy
    LEFT JOIN admin.Users vu ON vu.Id = lr.VerifiedBy
    LEFT JOIN admin.Users fu ON fu.Id = lr.FinalizedBy
    LEFT JOIN diag.Specimens sp ON sp.Id = ti.SpecimenId
    WHERE dt.InvestigationId = @InvestigationId AND dt.IsDeleted = 0
      AND (dt.Status IN ('Finalized', 'Delivered') OR (@Preview = 1 AND dt.Status = 'Verified'))
      AND (NOT EXISTS (SELECT 1 FROM @ids) OR dt.Id IN (SELECT Id FROM @ids))
    ORDER BY pg.SortOrder, tg.SortOrder, dt.Id;

    SELECT lr.InvestigationDetailId AS DetailId, rp.Name AS ParameterName, rp.GroupHeading, res.ResultValue, rp.Unit,
           CASE WHEN p.Gender = 'Male' AND ISNULL(rp.NormalRangeMale, '') <> '' THEN rp.NormalRangeMale
                WHEN p.Gender = 'Female' AND ISNULL(rp.NormalRangeFemale, '') <> '' THEN rp.NormalRangeFemale
                ELSE rp.NormalRange END AS NormalRange,
           res.Flag, rp.Method, res.Remarks, rp.SortOrder
    FROM diag.LabResults res
    JOIN diag.LabReports lr ON lr.Id = res.LabReportId AND lr.IsDeleted = 0
    JOIN diag.InvestigationDetails dt ON dt.Id = lr.InvestigationDetailId
    JOIN diag.Investigations i ON i.Id = dt.InvestigationId
    JOIN diag.Patients p ON p.Id = i.PatientId
    JOIN diag.ReportParameters rp ON rp.Id = res.ReportParameterId
    WHERE dt.InvestigationId = @InvestigationId AND res.IsDeleted = 0
      AND (dt.Status IN ('Finalized', 'Delivered') OR (@Preview = 1 AND dt.Status = 'Verified'))
      AND (NOT EXISTS (SELECT 1 FROM @ids) OR dt.Id IN (SELECT Id FROM @ids))
    ORDER BY lr.InvestigationDetailId, rp.SortOrder, rp.Id;
END
GO

-- Pathology sticker (barcode label) – one row per sample tube
CREATE OR ALTER PROCEDURE diag.usp_Rpt_PathologySticker
    @InvestigationId INT = NULL,
    @SampleId        INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sc.Id, sc.SampleNo, i.InvoiceNo, p.PatientCode, p.Name AS PatientName, p.Gender, i.PatientAge, sc.CollectedAt,
           v.Name AS Vacutainer, v.ColorHex, sp.Name AS Specimen,
           STUFF((SELECT ', ' + ISNULL(ti.ShortName, ti.Name)
                  FROM diag.InvestigationDetails dt JOIN diag.TestItems ti ON ti.Id = dt.TestItemId
                  WHERE dt.SampleCollectionId = sc.Id AND dt.IsDeleted = 0 AND dt.Status <> 'Cancelled'
                  FOR XML PATH('')), 1, 2, '') AS Tests
    FROM diag.SampleCollections sc
    JOIN diag.Investigations i ON i.Id = sc.InvestigationId
    JOIN diag.Patients p ON p.Id = i.PatientId
    LEFT JOIN diag.Vacutainers v ON v.Id = sc.VacutainerId
    LEFT JOIN diag.Specimens sp ON sp.Id = sc.SpecimenId
    WHERE sc.IsDeleted = 0 AND sc.Status <> 'Rejected'
      AND (@SampleId IS NULL OR sc.Id = @SampleId)
      AND (@InvestigationId IS NULL OR sc.InvestigationId = @InvestigationId)
      AND (@SampleId IS NOT NULL OR @InvestigationId IS NOT NULL)
    ORDER BY sc.Id;
END
GO

-- ---------------- Tabular (MIS) reports: one result set, rendered as a grid by the report server ----------------

CREATE OR ALTER PROCEDURE diag.usp_Rpt_DailyCollection
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(VARCHAR(10), py.PaymentDate, 103) AS [Date], py.ReceiptNo AS [Receipt No], i.InvoiceNo AS [Invoice No],
           p.Name AS [Patient], py.PaymentType AS [Type], pm.Name AS [Mode], ISNULL(pc.Name, '') AS [Channel],
           CASE WHEN py.PaymentType = 'Refund' THEN -py.Amount ELSE py.Amount END AS [Amount],
           ISNULL(u.FullName, '') AS [Received By]
    FROM diag.InvestigationPayments py
    JOIN diag.Investigations i ON i.Id = py.InvestigationId
    JOIN diag.Patients p ON p.Id = i.PatientId
    JOIN admin.PaymentModes pm ON pm.Id = py.PaymentModeId
    LEFT JOIN admin.PaymentChannels pc ON pc.Id = py.PaymentChannelId
    LEFT JOIN admin.Users u ON u.Id = py.CreatedBy
    WHERE py.IsDeleted = 0 AND py.PaymentDate >= @FromDate AND py.PaymentDate < DATEADD(DAY, 1, @ToDate)
    ORDER BY py.PaymentDate;
END
GO

CREATE OR ALTER PROCEDURE diag.usp_Rpt_TestWiseIncome
    @FromDate    DATE,
    @ToDate      DATE,
    @TestGroupId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tg.Name AS [Test Group], ti.Name AS [Test], COUNT(*) AS [Qty],
           SUM(dt.UnitPrice) AS [Gross], SUM(dt.DiscountAmount) AS [Discount], SUM(dt.VatAmount) AS [VAT], SUM(dt.NetAmount) AS [Net]
    FROM diag.InvestigationDetails dt
    JOIN diag.Investigations i ON i.Id = dt.InvestigationId
    JOIN diag.TestItems ti ON ti.Id = dt.TestItemId
    JOIN diag.TestGroups tg ON tg.Id = ti.TestGroupId
    WHERE dt.IsDeleted = 0 AND dt.Status <> 'Cancelled'
      AND i.InvoiceDate >= @FromDate AND i.InvoiceDate < DATEADD(DAY, 1, @ToDate)
      AND (@TestGroupId IS NULL OR tg.Id = @TestGroupId)
    GROUP BY tg.Name, tg.SortOrder, ti.Name
    ORDER BY tg.SortOrder, tg.Name, ti.Name;
END
GO

CREATE OR ALTER PROCEDURE diag.usp_Rpt_DueList
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.InvoiceNo AS [Invoice No], CONVERT(VARCHAR(10), i.InvoiceDate, 103) AS [Date], p.PatientCode AS [Patient ID],
           p.Name AS [Patient], ISNULL(p.Phone, '') AS [Phone], i.NetAmount AS [Net], i.PaidAmount AS [Paid],
           i.RefundAmount AS [Refund], i.DueAmount AS [Due]
    FROM diag.Investigations i
    JOIN diag.Patients p ON p.Id = i.PatientId
    WHERE i.IsDeleted = 0 AND i.DueAmount > 0
      AND i.InvoiceDate >= @FromDate AND i.InvoiceDate < DATEADD(DAY, 1, @ToDate)
    ORDER BY i.InvoiceDate;
END
GO

CREATE OR ALTER PROCEDURE diag.usp_Rpt_CancelRefund
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(VARCHAR(16), cr.CreatedAt, 120) AS [Requested], i.InvoiceNo AS [Invoice No], p.Name AS [Patient],
           ISNULL(ti.Name, 'Full invoice') AS [Test], cr.Reason, cr.RefundAmount AS [Refund], cr.Status,
           ISNULL(ru.FullName, '') AS [Requested By], ISNULL(au.FullName, '') AS [Approved By], ISNULL(cr.ApprovalRemarks, '') AS [Remarks]
    FROM diag.CancelRequests cr
    JOIN diag.Investigations i ON i.Id = cr.InvestigationId
    JOIN diag.Patients p ON p.Id = i.PatientId
    LEFT JOIN diag.InvestigationDetails dt ON dt.Id = cr.InvestigationDetailId
    LEFT JOIN diag.TestItems ti ON ti.Id = dt.TestItemId
    LEFT JOIN admin.Users ru ON ru.Id = cr.CreatedBy
    LEFT JOIN admin.Users au ON au.Id = cr.ApprovedBy
    WHERE cr.IsDeleted = 0 AND cr.CreatedAt >= @FromDate AND cr.CreatedAt < DATEADD(DAY, 1, @ToDate)
    ORDER BY cr.CreatedAt;
END
GO

CREATE OR ALTER PROCEDURE diag.usp_Rpt_DoctorWiseReferral
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(d.Name, 'Self') AS [Doctor], COUNT(DISTINCT i.Id) AS [Invoices], SUM(i.NetAmount) AS [Net Amount],
           ISNULL(MAX(d.CommissionPercent), 0) AS [Commission %],
           CAST(SUM(i.NetAmount) * ISNULL(MAX(d.CommissionPercent), 0) / 100 AS DECIMAL(18, 2)) AS [Commission]
    FROM diag.Investigations i
    LEFT JOIN diag.Doctors d ON d.Id = i.ReferDoctorId
    WHERE i.IsDeleted = 0 AND i.Status <> 'Cancelled'
      AND i.InvoiceDate >= @FromDate AND i.InvoiceDate < DATEADD(DAY, 1, @ToDate)
    GROUP BY d.Name
    ORDER BY SUM(i.NetAmount) DESC;
END
GO

-- ---------------- Media commission ----------------

CREATE OR ALTER PROCEDURE diag.usp_Rpt_MediaCommissionSummary
    @FromDate DATE,
    @ToDate   DATE,
    @MediaId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Code AS [Code], m.Name AS [Media], ISNULL(m.Phone, '') AS [Phone],
           COUNT(DISTINCT i.Id) AS [Invoices], COUNT(dt.Id) AS [Tests],
           SUM(dt.UnitPrice - dt.DiscountAmount) AS [Bill (after discount)], SUM(dt.MediaCommission) AS [Commission]
    FROM diag.Investigations i
    JOIN diag.Medias m ON m.Id = i.MediaId
    JOIN diag.InvestigationDetails dt ON dt.InvestigationId = i.Id AND dt.IsDeleted = 0 AND dt.Status <> 'Cancelled'
    WHERE i.IsDeleted = 0 AND i.InvoiceDate >= @FromDate AND i.InvoiceDate < DATEADD(DAY, 1, @ToDate)
      AND (@MediaId IS NULL OR i.MediaId = @MediaId)
    GROUP BY m.Code, m.Name, m.Phone
    ORDER BY SUM(dt.MediaCommission) DESC;
END
GO

CREATE OR ALTER PROCEDURE diag.usp_Rpt_MediaCommissionDetail
    @FromDate DATE,
    @ToDate   DATE,
    @MediaId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Name AS [Media], CONVERT(VARCHAR(10), i.InvoiceDate, 103) AS [Date], i.InvoiceNo AS [Invoice No], p.Name AS [Patient],
           ti.Name AS [Test], tg.Name AS [Group], dt.UnitPrice - dt.DiscountAmount AS [Bill (after discount)],
           dt.MediaCommission AS [Commission]
    FROM diag.Investigations i
    JOIN diag.Medias m ON m.Id = i.MediaId
    JOIN diag.Patients p ON p.Id = i.PatientId
    JOIN diag.InvestigationDetails dt ON dt.InvestigationId = i.Id AND dt.IsDeleted = 0 AND dt.Status <> 'Cancelled'
    JOIN diag.TestItems ti ON ti.Id = dt.TestItemId
    JOIN diag.TestGroups tg ON tg.Id = ti.TestGroupId
    WHERE i.IsDeleted = 0 AND i.InvoiceDate >= @FromDate AND i.InvoiceDate < DATEADD(DAY, 1, @ToDate)
      AND (@MediaId IS NULL OR i.MediaId = @MediaId)
    ORDER BY m.Name, i.InvoiceDate, dt.Id;
END
GO
