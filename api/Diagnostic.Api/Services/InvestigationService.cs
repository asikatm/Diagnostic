using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Services;

// ------------------------------------------------------------------ DTOs

public class PatientInput
{
    public string? Title { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public int? ReligionId { get; set; }
    public int? CountryId { get; set; }
}

public class InvestigationLineInput
{
    public int TestItemId { get; set; }
    public int Quantity { get; set; } = 1;
    /// <summary>Manual line discount as a flat amount …</summary>
    public decimal DiscountAmount { get; set; }
    /// <summary>… or as a percent of the line amount (wins when &gt; 0).</summary>
    public decimal DiscountPercent { get; set; }
    public bool IsUrgent { get; set; }
    public string? Note { get; set; }
}

public class PaymentInput
{
    public decimal Amount { get; set; }
    public int PaymentModeId { get; set; }
    public int? PaymentChannelId { get; set; }
    public string? TransactionRef { get; set; }
}

public class CreateInvestigationDto
{
    /// <summary>Existing patient; when null <see cref="Patient"/> is registered.</summary>
    public int? PatientId { get; set; }
    public PatientInput? Patient { get; set; }
    public string? PatientAge { get; set; }
    public int? ReferDoctorId { get; set; }
    /// <summary>Media / agent who brought the patient (earns commission).</summary>
    public int? MediaId { get; set; }
    public int? MarketingAreaId { get; set; }
    public int? OpdPackageId { get; set; }
    public int? DiscountSetupId { get; set; }
    public List<InvestigationLineInput> Tests { get; set; } = [];
    /// <summary>Invoice-level flat discount, spread over discountable tests.</summary>
    public decimal ExtraDiscount { get; set; }
    public PaymentInput? Payment { get; set; }
    /// <summary>Split payment (cash, card, mobile …) – added to <see cref="Payment"/>.</summary>
    public List<PaymentInput> Payments { get; set; } = [];
    /// <summary>Required when any discount is applied.</summary>
    public string? Remarks { get; set; }
}

public record InvoiceListQuery(DateTime? From, DateTime? To, string? Search, bool DueOnly = false, int Page = 1, int PageSize = 50);

public record InvoiceListItem(int Id, string InvoiceNo, DateTime InvoiceDate, string PatientCode, string PatientName, string? Phone,
    string? ReferDoctor, decimal NetAmount, decimal PaidAmount, decimal RefundAmount, decimal DueAmount, InvestigationStatus Status, int TestCount);

public record InvoiceDetailDto(int Id, int TestItemId, string TestCode, string TestName, string GroupName, decimal UnitPrice,
    decimal DiscountAmount, decimal VatAmount, decimal NetAmount, TestStatus Status, DateTime? DeliveryDateTime, string? SampleNo,
    int Quantity, bool IsUrgent, string? Note, bool IsConsumable, int? ParentDetailId);

public record InvoicePaymentDto(int Id, string ReceiptNo, DateTime PaymentDate, PaymentType PaymentType, string PaymentMode,
    string? PaymentChannel, decimal Amount, string? TransactionRef);

public record InvoiceDto(int Id, string InvoiceNo, DateTime InvoiceDate, int PatientId, string PatientCode, string PatientName,
    Gender Gender, string? PatientAge, string? Phone, string? Address, string? ReferDoctor, string? PackageName,
    decimal GrossAmount, decimal DiscountAmount, decimal VatAmount, decimal NetAmount, decimal PaidAmount, decimal RefundAmount,
    decimal DueAmount, InvestigationStatus Status, string? Remarks, List<InvoiceDetailDto> Details, List<InvoicePaymentDto> Payments,
    string? MediaName, decimal MediaCommission);

public record CreateInvestigationResult(int InvestigationId, string InvoiceNo, int? PaymentId, string? ReceiptNo, decimal NetAmount, decimal DueAmount);

// ------------------------------------------------------------------ Service

public class InvestigationService(DiagnosticDbContext db, NumberSequenceService numbers, ICurrentUser currentUser)
{
    public async Task<CreateInvestigationResult> CreateAsync(CreateInvestigationDto dto)
    {
        if (dto.Tests.Count == 0 && dto.OpdPackageId is null) throw new AppException("Add at least one test or a package.");
        if (dto.ExtraDiscount < 0) throw new AppException("Discount cannot be negative.");

        await using var tx = await db.Database.BeginTransactionAsync();

        var patient = await ResolvePatientAsync(dto);
        var lines = await BuildLinesAsync(dto);
        await ApplyDiscountsAsync(dto, lines);

        var inv = new Investigation
        {
            InvoiceNo = await numbers.NextAsync(NumberSequenceService.Invoice),
            InvoiceDate = DateTime.Now,
            TenantId = currentUser.TenantId ?? await db.Tenants.OrderBy(t => t.Id).Select(t => t.Id).FirstAsync(),
            Patient = patient,
            PatientAge = dto.PatientAge ?? AgeText(patient.DateOfBirth),
            ReferDoctorId = dto.ReferDoctorId,
            MediaId = dto.MediaId,
            MarketingAreaId = dto.MarketingAreaId,
            OpdPackageId = dto.OpdPackageId,
            DiscountSetupId = dto.DiscountSetupId,
            Remarks = dto.Remarks,
        };

        if (lines.Any(l => l.Discount > 0) && string.IsNullOrWhiteSpace(dto.Remarks))
            throw new AppException("Remarks is required when any discount is applied.");
        await CheckDiscountPermissionAsync(lines);

        var delivery = await DeliveryCalculator.CreateAsync(db, lines.Select(l => l.Item).ToList());
        var commission = dto.MediaId.HasValue ? await MediaCommissionCalculator.CreateAsync(db, dto.MediaId.Value) : null;
        var detailOf = new Dictionary<Line, InvestigationDetail>();
        foreach (var l in lines)
        {
            var taxable = l.Amount - l.Discount;
            var vat = Math.Round(taxable * l.Item.VatPercent / 100m, 2);
            var detail = new InvestigationDetail
            {
                TestItemId = l.Item.Id,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                IsUrgent = l.IsUrgent,
                Note = l.Note,
                DiscountAmount = l.Discount,
                VatAmount = vat,
                NetAmount = taxable + vat,
                MediaCommission = l.Parent == null ? commission?.For(l.Item, taxable) ?? 0 : 0,
                DeliveryDateTime = l.Item.IsConsumable ? null : delivery.For(l.Item, inv.InvoiceDate),
            };
            detailOf[l] = detail;
            inv.Details.Add(detail);
        }

        InvoiceTotals.Recalculate(inv);

        // Cash + card + mobile banking can be taken together; each becomes its own money receipt.
        var payments = new List<PaymentInput>(dto.Payments);
        if (dto.Payment != null) payments.Insert(0, dto.Payment);
        payments = payments.Where(p => p.Amount > 0).ToList();
        if (payments.Sum(p => p.Amount) > inv.NetAmount) throw new AppException("Paid amount cannot be more than the net amount.");
        foreach (var p in payments) inv.Payments.Add(await NewPaymentAsync(p, PaymentType.Payment));
        InvoiceTotals.Recalculate(inv);

        db.Investigations.Add(inv);
        await db.SaveChangesAsync();

        // Link consumable lines to their test line (ids exist only after the first save).
        foreach (var (line, detail) in detailOf.Where(x => x.Key.Parent != null))
            detail.ParentDetailId = detailOf[line.Parent!].Id;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var single = inv.Payments.Count == 1 ? inv.Payments[0] : null; // several receipts → print the whole invoice
        return new CreateInvestigationResult(inv.Id, inv.InvoiceNo, single?.Id, string.Join(", ", inv.Payments.Select(p => p.ReceiptNo)),
            inv.NetAmount, inv.DueAmount);
    }

    public async Task<InvestigationPayment> CollectDueAsync(int investigationId, PaymentInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var inv = await db.Investigations.Include(i => i.Details).Include(i => i.Payments)
                      .FirstOrDefaultAsync(i => i.Id == investigationId) ?? throw new AppException("Invoice not found.", 404);
        if (inv.Status == InvestigationStatus.Cancelled) throw new AppException("Invoice is cancelled.");
        if (input.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        if (input.Amount > inv.DueAmount) throw new AppException($"Amount cannot be more than the due ({inv.DueAmount:N2}).");

        var payment = await NewPaymentAsync(input, PaymentType.DueCollection);
        inv.Payments.Add(payment);
        InvoiceTotals.Recalculate(inv);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return payment;
    }

    public async Task<PagedResult<InvoiceListItem>> ListAsync(InvoiceListQuery q)
    {
        var query = db.Investigations.AsNoTracking().AsQueryable();
        if (q.From.HasValue) query = query.Where(i => i.InvoiceDate >= q.From.Value.Date);
        if (q.To.HasValue) query = query.Where(i => i.InvoiceDate < q.To.Value.Date.AddDays(1));
        if (q.DueOnly) query = query.Where(i => i.DueAmount > 0);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(i => i.InvoiceNo.Contains(s) || i.Patient!.Name.Contains(s) || i.Patient.Phone!.Contains(s) || i.Patient.PatientCode == s);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(i => i.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(i => new InvoiceListItem(i.Id, i.InvoiceNo, i.InvoiceDate, i.Patient!.PatientCode, i.Patient.Name, i.Patient.Phone,
                i.ReferDoctor!.Name, i.NetAmount, i.PaidAmount, i.RefundAmount, i.DueAmount, i.Status, i.Details.Count))
            .ToListAsync();
        return new PagedResult<InvoiceListItem> { Items = items, Total = total, Page = q.Page, PageSize = q.PageSize };
    }

    public async Task<InvoiceDto?> GetAsync(int? id, string? invoiceNo = null)
    {
        var q = db.Investigations.AsNoTracking();
        q = id.HasValue ? q.Where(i => i.Id == id) : q.Where(i => i.InvoiceNo == invoiceNo);
        return await q.Select(i => new InvoiceDto(
                i.Id, i.InvoiceNo, i.InvoiceDate, i.PatientId, i.Patient!.PatientCode, i.Patient.Name, i.Patient.Gender, i.PatientAge,
                i.Patient.Phone, i.Patient.Address, i.ReferDoctor!.Name, i.OpdPackage!.Name,
                i.GrossAmount, i.DiscountAmount, i.VatAmount, i.NetAmount, i.PaidAmount, i.RefundAmount, i.DueAmount, i.Status, i.Remarks,
                i.Details.OrderBy(d => d.Id).Select(d => new InvoiceDetailDto(d.Id, d.TestItemId, d.TestItem!.Code ?? "", d.TestItem.Name,
                    d.TestItem.TestGroup!.Name, d.UnitPrice, d.DiscountAmount, d.VatAmount, d.NetAmount, d.Status, d.DeliveryDateTime,
                    d.SampleCollection!.SampleNo, d.Quantity, d.IsUrgent, d.Note, d.TestItem.IsConsumable, d.ParentDetailId)).ToList(),
                i.Payments.OrderBy(p => p.Id).Select(p => new InvoicePaymentDto(p.Id, p.ReceiptNo, p.PaymentDate, p.PaymentType,
                    p.PaymentMode!.Name, p.PaymentChannel!.Name, p.Amount, p.TransactionRef)).ToList(),
                i.Media!.Name, i.MediaCommission))
            .FirstOrDefaultAsync();
    }

    // ---------------------------------------------------------------- helpers

    internal async Task<InvestigationPayment> NewPaymentAsync(PaymentInput input, PaymentType type)
    {
        var mode = await db.PaymentModes.AsNoTracking().FirstOrDefaultAsync(m => m.Id == input.PaymentModeId && m.IsActive)
                   ?? throw new AppException("Select a valid payment mode.");
        if (mode.RequiresReference && string.IsNullOrWhiteSpace(input.TransactionRef))
            throw new AppException($"Transaction reference is required for {mode.Name}.");
        if (input.PaymentChannelId.HasValue &&
            !await db.PaymentChannels.AnyAsync(c => c.Id == input.PaymentChannelId && c.PaymentModeId == mode.Id))
            throw new AppException("Payment channel does not belong to the selected payment mode.");

        return new InvestigationPayment
        {
            ReceiptNo = await numbers.NextAsync(NumberSequenceService.Receipt),
            PaymentDate = DateTime.Now,
            PaymentType = type,
            PaymentModeId = mode.Id,
            PaymentChannelId = input.PaymentChannelId,
            Amount = Math.Round(input.Amount, 2),
            TransactionRef = input.TransactionRef,
        };
    }

    private async Task<Patient> ResolvePatientAsync(CreateInvestigationDto dto)
    {
        if (dto.PatientId.HasValue)
            return await db.Patients.FirstOrDefaultAsync(p => p.Id == dto.PatientId) ?? throw new AppException("Patient not found.");

        var p = dto.Patient ?? throw new AppException("Patient information is required.");
        if (string.IsNullOrWhiteSpace(p.Name)) throw new AppException("Patient name is required.");
        var patient = new Patient
        {
            PatientCode = await numbers.NextAsync(NumberSequenceService.Patient),
            Title = string.IsNullOrWhiteSpace(p.Title) ? null : p.Title.Trim(), Name = p.Name.Trim(), Gender = p.Gender, DateOfBirth = p.DateOfBirth, Phone = p.Phone, Address = p.Address,
            BloodGroup = p.BloodGroup, ReligionId = p.ReligionId, CountryId = p.CountryId,
        };
        db.Patients.Add(patient);
        return patient;
    }

    /// <summary>
    /// Discount permission: highest discount % (of the discountable amount) the user's roles allow.
    /// Super admin or any role without a limit = no limit; the most generous role wins.
    /// </summary>
    public async Task<decimal?> MaxDiscountPercentAsync()
    {
        if (currentUser.IsSuperAdmin) return null;
        var roleIds = currentUser.RoleIds;
        var limits = await db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id) && r.IsActive)
            .Select(r => r.MaxDiscountPercent).ToListAsync();
        return limits.Count == 0 || limits.Any(l => l == null) ? null : limits.Max();
    }

    private async Task CheckDiscountPermissionAsync(List<Line> lines)
    {
        var discount = lines.Sum(l => l.Discount);
        if (discount <= 0) return;
        var limit = await MaxDiscountPercentAsync();
        if (limit is null) return;
        var discountable = lines.Where(l => l.MaxDiscount > 0).Sum(l => l.Amount);
        var percent = discountable == 0 ? 100 : Math.Round(discount * 100m / discountable, 2);
        if (percent > limit)
            throw new AppException($"Your discount permission is {limit:0.##}% of the discountable amount ({discountable * limit.Value / 100m:N2} Tk). " +
                                   $"This bill gives {percent:0.##}% ({discount:N2} Tk).", StatusCodes.Status403Forbidden);
    }

    private sealed class Line
    {
        public required TestItem Item { get; init; }
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; } = 1;
        public bool IsUrgent { get; init; }
        public string? Note { get; init; }
        public decimal Discount { get; set; }
        public bool FromPackage { get; init; }
        /// <summary>Consumable added for the test line at this index (needle, tourniquet …).</summary>
        public Line? Parent { get; init; }
        public decimal Amount => UnitPrice * Quantity;
        public decimal MaxDiscount => FromPackage || Parent != null || !Item.IsDiscountAllowed
            ? 0 : Math.Round(Amount * Item.MaxDiscountPercent / 100m, 2);
    }

    private async Task<List<Line>> BuildLinesAsync(CreateInvestigationDto dto)
    {
        var lines = new List<Line>();
        if (dto.OpdPackageId.HasValue)
        {
            var pkg = await db.OpdPackages.AsNoTracking().Include(p => p.Items).ThenInclude(i => i.TestItem)
                          .FirstOrDefaultAsync(p => p.Id == dto.OpdPackageId && p.IsActive) ?? throw new AppException("Package not found.");
            var today = DateOnly.FromDateTime(DateTime.Today);
            if ((pkg.ValidFrom.HasValue && pkg.ValidFrom > today) || (pkg.ValidTo.HasValue && pkg.ValidTo < today))
                throw new AppException($"Package '{pkg.Name}' is not valid today.");
            lines.AddRange(pkg.Items.Select(i => new Line { Item = i.TestItem!, UnitPrice = i.ItemPrice, FromPackage = true }));
        }

        var ids = dto.Tests.Select(t => t.TestItemId).ToList();
        if (ids.Distinct().Count() != ids.Count) throw new AppException("A test is added twice.");
        var items = await db.TestItems.AsNoTracking().Where(t => ids.Contains(t.Id) && t.IsActive).ToDictionaryAsync(t => t.Id);

        foreach (var t in dto.Tests)
        {
            if (!items.TryGetValue(t.TestItemId, out var item)) throw new AppException($"Test #{t.TestItemId} is not available.");
            if (lines.Any(l => l.Item.Id == item.Id)) throw new AppException($"'{item.Name}' is already included in the package.");
            if (t.Quantity is < 1 or > 100) throw new AppException($"Quantity of '{item.Name}' must be between 1 and 100.");
            var line = new Line { Item = item, UnitPrice = item.Price, Quantity = t.Quantity, IsUrgent = t.IsUrgent, Note = t.Note?.Trim() };
            if (t.DiscountAmount < 0 || t.DiscountPercent is < 0 or > 100) throw new AppException("Discount cannot be negative or above 100 %.");
            var discount = t.DiscountPercent > 0 ? Math.Round(line.Amount * t.DiscountPercent / 100m, 2) : Math.Round(t.DiscountAmount, 2);
            if (discount > line.MaxDiscount) throw new AppException($"Maximum discount for '{item.Name}' is {line.MaxDiscount:N2}.");
            line.Discount = discount;
            lines.Add(line);
        }

        // Needle & tourniquet mapping: consumables are billed automatically with their test (package tests included).
        var testIds = lines.Select(l => l.Item.Id).ToList();
        var maps = await db.TestItemConsumables.AsNoTracking().Include(m => m.ConsumableItem)
            .Where(m => m.IsActive && testIds.Contains(m.TestItemId) && m.ConsumableItem!.IsActive).ToListAsync();
        foreach (var parent in lines.ToList())
            foreach (var m in maps.Where(m => m.TestItemId == parent.Item.Id))
                lines.Add(new Line { Item = m.ConsumableItem!, UnitPrice = m.ConsumableItem!.Price, Quantity = m.Quantity, Parent = parent });
        return lines;
    }

    private async Task ApplyDiscountsAsync(CreateInvestigationDto dto, List<Line> lines)
    {
        if (dto.DiscountSetupId.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var setup = await db.DiscountSetups.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dto.DiscountSetupId && d.IsActive
                            && (d.ValidFrom == null || d.ValidFrom <= today) && (d.ValidTo == null || d.ValidTo >= today))
                        ?? throw new AppException("Selected discount is not valid today.");
            if (setup.DoctorId.HasValue && setup.DoctorId != dto.ReferDoctorId)
                throw new AppException($"Discount '{setup.Name}' is only for patients referred by the configured doctor.");

            // Test wise (TestItemId) and/or group wise (TestGroupId) scheme; both empty = all tests.
            var eligible = lines.Where(l => l.Parent == null && (setup.TestItemId == null || l.Item.Id == setup.TestItemId)
                                            && (setup.TestGroupId == null || l.Item.TestGroupId == setup.TestGroupId)).ToList();
            if (eligible.Count == 0) throw new AppException($"Discount '{setup.Name}' does not apply to any selected test.");
            if (setup.DiscountType == DiscountType.Percent)
                foreach (var l in eligible)
                    l.Discount = Math.Min(l.MaxDiscount, l.Discount + Math.Round(l.Amount * setup.Value / 100m, 2));
            else
                Spread(eligible, setup.Value, strict: false);
        }

        if (dto.ExtraDiscount > 0) Spread(lines, dto.ExtraDiscount, strict: true);
    }

    /// <summary>Spreads a flat discount over lines in proportion to price, respecting each line's maximum.</summary>
    private static void Spread(List<Line> lines, decimal amount, bool strict)
    {
        var room = lines.Sum(l => l.MaxDiscount - l.Discount);
        if (amount > room)
        {
            if (strict) throw new AppException($"Maximum additional discount allowed is {room:N2}.");
            amount = room;
        }
        var candidates = lines.Where(l => l.MaxDiscount - l.Discount > 0).ToList();
        var basis = candidates.Sum(l => l.Amount);
        var remaining = amount;
        for (var i = 0; i < candidates.Count && remaining > 0; i++)
        {
            var l = candidates[i];
            var share = i == candidates.Count - 1 ? remaining : Math.Round(amount * l.Amount / basis, 2);
            share = Math.Min(share, Math.Min(remaining, l.MaxDiscount - l.Discount));
            l.Discount += share;
            remaining -= share;
        }
        // Rounding / caps may leave a few paisa – put them on any line with room.
        foreach (var l in lines)
        {
            if (remaining <= 0) break;
            var add = Math.Min(remaining, l.MaxDiscount - l.Discount);
            l.Discount += add;
            remaining -= add;
        }
    }

    public static string? AgeText(DateOnly? dob)
    {
        if (dob is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var years = today.Year - dob.Value.Year - (today.DayOfYear < dob.Value.DayOfYear ? 1 : 0);
        if (years >= 1) return $"{years} Y";
        var months = (today.Year - dob.Value.Year) * 12 + today.Month - dob.Value.Month;
        return months >= 1 ? $"{months} M" : $"{today.DayNumber - dob.Value.DayNumber} D";
    }
}

/// <summary>Recomputes invoice header amounts from its non-cancelled lines and its payments.</summary>
public static class InvoiceTotals
{
    public static void Recalculate(Investigation inv)
    {
        var active = inv.Details.Where(d => d.Status != TestStatus.Cancelled && !d.IsDeleted).ToList();
        inv.GrossAmount = active.Sum(d => d.UnitPrice * d.Quantity);
        inv.DiscountAmount = active.Sum(d => d.DiscountAmount);
        inv.VatAmount = active.Sum(d => d.VatAmount);
        inv.NetAmount = active.Sum(d => d.NetAmount);
        inv.MediaCommission = active.Sum(d => d.MediaCommission); // cancelled tests earn no commission
        inv.PaidAmount = inv.Payments.Where(p => p.PaymentType != PaymentType.Refund && !p.IsDeleted).Sum(p => p.Amount);
        inv.RefundAmount = inv.Payments.Where(p => p.PaymentType == PaymentType.Refund && !p.IsDeleted).Sum(p => p.Amount);
        inv.DueAmount = inv.NetAmount - inv.PaidAmount + inv.RefundAmount;
        inv.Status = active.Count == 0 && inv.Details.Count > 0 ? InvestigationStatus.Cancelled
            : active.Count < inv.Details.Count ? InvestigationStatus.PartiallyCancelled
            : InvestigationStatus.Active;
    }
}

/// <summary>Resolves report delivery date/time: item rule → group rule → global default.</summary>
public class DeliveryCalculator
{
    private readonly List<ReportDeliveryTime> _rules;
    private readonly int _defaultHours;

    private DeliveryCalculator(List<ReportDeliveryTime> rules, int defaultHours)
    {
        _rules = rules;
        _defaultHours = defaultHours;
    }

    public static async Task<DeliveryCalculator> CreateAsync(DiagnosticDbContext db, List<TestItem> items)
    {
        var itemIds = items.Select(i => i.Id).ToList();
        var groupIds = items.Select(i => i.TestGroupId).Distinct().ToList();
        var rules = await db.ReportDeliveryTimes.AsNoTracking()
            .Where(r => r.IsActive && ((r.TestItemId != null && itemIds.Contains(r.TestItemId.Value)) ||
                                       (r.TestItemId == null && r.TestGroupId != null && groupIds.Contains(r.TestGroupId.Value))))
            .ToListAsync();
        var setting = await db.AppGlobalSettings.AsNoTracking()
            .Where(s => s.SettingKey == "Diag.DefaultDeliveryHours").Select(s => s.SettingValue).FirstOrDefaultAsync();
        return new DeliveryCalculator(rules, int.TryParse(setting, out var h) ? h : 24);
    }

    public DateTime For(TestItem item, DateTime billedAt)
    {
        var rule = _rules.FirstOrDefault(r => r.TestItemId == item.Id)
                   ?? _rules.FirstOrDefault(r => r.TestItemId == null && r.TestGroupId == item.TestGroupId);
        var at = billedAt.AddHours(rule?.DeliveryHours ?? _defaultHours);
        if (rule?.DeliveryAt is { } clock)
        {
            at = at.Date + clock.ToTimeSpan();
            if (at < billedAt) at = at.AddDays(1);
        }
        return at;
    }
}

/// <summary>
/// Media commission of one billed test, on its price after discount (before VAT).
/// Order: test item rule → test group rule → media's default %. For each level a rule for this media wins over a rule for all media.
/// </summary>
public class MediaCommissionCalculator
{
    private readonly List<MediaCommissionSetup> _rules;
    private readonly int _mediaId;
    private readonly decimal _defaultPercent;

    private MediaCommissionCalculator(List<MediaCommissionSetup> rules, int mediaId, decimal defaultPercent)
    {
        _rules = rules;
        _mediaId = mediaId;
        _defaultPercent = defaultPercent;
    }

    public static async Task<MediaCommissionCalculator> CreateAsync(DiagnosticDbContext db, int mediaId)
    {
        var media = await db.Medias.AsNoTracking().FirstOrDefaultAsync(m => m.Id == mediaId && m.IsActive)
                    ?? throw new AppException("Selected media is not available.");
        var rules = await db.MediaCommissionSetups.AsNoTracking()
            .Where(r => r.IsActive && (r.MediaId == mediaId || r.MediaId == null)).ToListAsync();
        return new MediaCommissionCalculator(rules, mediaId, media.DefaultCommissionPercent);
    }

    public decimal For(TestItem item, decimal amount)
    {
        var rule = Pick(r => r.TestItemId == item.Id)
                   ?? Pick(r => r.TestItemId == null && r.TestGroupId == item.TestGroupId);
        var value = rule is null
            ? amount * _defaultPercent / 100m
            : rule.CommissionType == DiscountType.Percent ? amount * rule.Value / 100m : Math.Min(rule.Value, amount);
        return Math.Round(Math.Max(0, value), 2);
    }

    private MediaCommissionSetup? Pick(Func<MediaCommissionSetup, bool> match) =>
        _rules.Where(match).OrderByDescending(r => r.MediaId == _mediaId).FirstOrDefault();
}
