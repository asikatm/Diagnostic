using System.Data;
using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Diagnostic.Report.Data;

public class ReportException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Reads report definitions (admin.Reports) and executes the mapped stored procedures.</summary>
public class ReportRepository(IConfiguration config)
{
    private SqlConnection Open() => new(config.GetConnectionString("DiagnosticDb"));

    public async Task<List<ReportDefinition>> ListAsync(bool superAdmin, IReadOnlyList<int> roleIds)
    {
        await using var cn = Open();
        var reports = (await cn.QueryAsync<ReportDefinition>("""
            SELECT r.Id, r.Code, r.Name, r.ProcedureName, r.Template, r.Orientation
            FROM admin.Reports r
            WHERE r.IsDeleted = 0 AND r.IsActive = 1
              AND (@superAdmin = 1 OR EXISTS (SELECT 1 FROM admin.ReportPermissions p
                   WHERE p.ReportId = r.Id AND p.IsDeleted = 0 AND p.CanView = 1 AND p.RoleId IN @roleIds))
            ORDER BY r.SortOrder, r.Name
            """, new { superAdmin, roleIds = roleIds.DefaultIfEmpty(0) })).ToList();

        var ps = (await cn.QueryAsync<ReportParam>("""
            SELECT ReportId, ParamName, Label, DataType, IsRequired, DefaultValue, SortOrder
            FROM admin.ReportSpParameters WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY SortOrder
            """)).ToLookup(p => p.ReportId);
        foreach (var r in reports) r.Parameters = ps[r.Id].ToList();
        return reports;
    }

    public async Task<ReportDefinition> GetAsync(string code, bool superAdmin, IReadOnlyList<int> roleIds, bool export = false)
    {
        var all = await ListAsync(superAdmin, roleIds);
        var r = all.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (r is null) throw new ReportException($"Report '{code}' not found or you do not have permission.", 403);

        if (export && !superAdmin)
        {
            await using var cn = Open();
            var ok = await cn.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM admin.ReportPermissions
                WHERE ReportId = @id AND IsDeleted = 0 AND CanExport = 1 AND RoleId IN @roleIds
                """, new { id = r.Id, roleIds = roleIds.DefaultIfEmpty(0) });
            if (ok == 0) throw new ReportException("You do not have export permission for this report.", 403);
        }
        return r;
    }

    public async Task<LetterHeadRow?> LetterHeadAsync(int? letterHeadId, int? tenantId)
    {
        await using var cn = Open();
        return await cn.QueryFirstOrDefaultAsync<LetterHeadRow>("diag.usp_Rpt_LetterHead",
            new { LetterHeadId = letterHeadId, TenantId = tenantId }, commandType: CommandType.StoredProcedure);
    }

    public async Task<MoneyReceiptData> MoneyReceiptAsync(ReportDefinition def, int investigationId, int? paymentId, int? letterHeadId)
    {
        await using var cn = Open();
        await using var grid = await cn.QueryMultipleAsync(def.ProcedureName,
            new { InvestigationId = investigationId, PaymentId = paymentId }, commandType: CommandType.StoredProcedure);
        var header = await grid.ReadFirstOrDefaultAsync<InvoiceHeaderRow>() ?? throw new ReportException("Invoice not found.", 404);
        var tests = (await grid.ReadAsync<InvoiceTestRow>()).ToList();
        var payments = (await grid.ReadAsync<InvoicePaymentRow>()).ToList();
        return new MoneyReceiptData(await LetterHeadAsync(letterHeadId, header.TenantId), header, tests, payments);
    }

    public async Task<LabReportData> LabReportAsync(ReportDefinition def, int investigationId, string? detailIds, int? letterHeadId, bool preview = false)
    {
        await using var cn = Open();
        await using var grid = await cn.QueryMultipleAsync(def.ProcedureName,
            new { InvestigationId = investigationId, DetailIds = detailIds, Preview = preview }, commandType: CommandType.StoredProcedure);
        var header = await grid.ReadFirstOrDefaultAsync<InvoiceHeaderRow>() ?? throw new ReportException("Invoice not found.", 404);
        var tests = (await grid.ReadAsync<LabTestRow>()).ToList();
        var results = (await grid.ReadAsync<LabResultRow>()).ToList();
        if (tests.Count == 0) throw new ReportException(preview
            ? "No verified report found for this invoice." : "No finalized report found – the pathologist must finalize it first.", 404);
        return new LabReportData(await LetterHeadAsync(letterHeadId, header.TenantId), header, tests, results);
    }

    public async Task MarkPrintedAsync(IEnumerable<int> detailIds)
    {
        await using var cn = Open();
        await cn.ExecuteAsync("UPDATE diag.LabReports SET PrintCount = PrintCount + 1 WHERE InvestigationDetailId IN @ids",
            new { ids = detailIds });
    }

    public async Task<List<StickerRow>> StickersAsync(ReportDefinition def, int? investigationId, int? sampleId)
    {
        if (investigationId is null && sampleId is null) throw new ReportException("Give an invoice or a sample.");
        await using var cn = Open();
        return (await cn.QueryAsync<StickerRow>(def.ProcedureName, new { InvestigationId = investigationId, SampleId = sampleId },
            commandType: CommandType.StoredProcedure)).ToList();
    }

    // Lookup parameter type → table/column holding its display name (fixed list, never built from user input).
    private static readonly Dictionary<string, string> LookupNameSql = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tenant"] = "SELECT Name FROM admin.Tenants WHERE Id = @id",
        ["user"] = "SELECT FullName FROM admin.Users WHERE Id = @id",
        ["paymentmode"] = "SELECT Name FROM admin.PaymentModes WHERE Id = @id",
        ["doctor"] = "SELECT Name FROM diag.Doctors WHERE Id = @id",
        ["media"] = "SELECT Name FROM diag.Medias WHERE Id = @id",
        ["parentgroup"] = "SELECT Name FROM diag.ParentGroups WHERE Id = @id",
        ["testgroup"] = "SELECT Name FROM diag.TestGroups WHERE Id = @id",
        ["testitem"] = "SELECT Name FROM diag.TestItems WHERE Id = @id",
        ["discount"] = "SELECT Name FROM diag.DiscountSetups WHERE Id = @id",
        ["package"] = "SELECT Name FROM diag.OpdPackages WHERE Id = @id",
    };

    /// <summary>Text shown in the report's filter line: names instead of ids, month names, formatted dates.</summary>
    private async Task<string> DisplayAsync(ReportParam p, object value)
    {
        if (value is DateTime d) return d.ToString("dd-MMM-yyyy");
        if (p.DataType.Equals("month", StringComparison.OrdinalIgnoreCase) && value is int m)
            return CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m);
        if (value is int id && LookupNameSql.TryGetValue(p.DataType, out var sql))
        {
            await using var cn = Open();
            return await cn.ExecuteScalarAsync<string?>(sql, new { id }) ?? $"#{id}";
        }
        return value.ToString() ?? "";
    }

    /// <summary>Runs a tabular report: query-string values are converted with the parameter definitions.</summary>
    public async Task<TabularData> TabularAsync(ReportDefinition def, IQueryCollection query)
    {
        var args = new DynamicParameters();
        var subtitle = new List<string>();
        foreach (var p in def.Parameters)
        {
            var name = p.ParamName.TrimStart('@');
            var raw = query.TryGetValue(name, out var v) || query.TryGetValue("@" + name, out v) ? v.ToString() : p.DefaultValue;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (p.IsRequired) throw new ReportException($"'{p.Label}' is required.");
                args.Add(name, null);
                continue;
            }
            var value = Convert(p, raw);
            args.Add(name, value);
            subtitle.Add($"{p.Label}: {await DisplayAsync(p, value)}");
        }

        await using var cn = Open();
        using var reader = await cn.ExecuteReaderAsync(def.ProcedureName, args, commandType: CommandType.StoredProcedure, commandTimeout: 120);
        var cols = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<object?[]>();
        while (await reader.ReadAsync())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < row.Length; i++) row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return new TabularData(def.Name, string.Join("   |   ", subtitle), cols, rows);
    }

    private static object Convert(ReportParam p, string raw)
    {
        try
        {
            // Lookup types (tenant, user, doctor, media, testgroup …) carry the selected row's Id.
            return p.DataType.ToLowerInvariant() switch
            {
                "string" => raw,
                "decimal" => decimal.Parse(raw, CultureInfo.InvariantCulture),
                "bool" => raw is "1" or "true" or "True",
                "date" => raw.Equals("today", StringComparison.OrdinalIgnoreCase) ? DateTime.Today : DateTime.Parse(raw, CultureInfo.InvariantCulture),
                "year" => raw.Equals("thisyear", StringComparison.OrdinalIgnoreCase) ? DateTime.Today.Year : int.Parse(raw, CultureInfo.InvariantCulture),
                "month" => raw.Equals("thismonth", StringComparison.OrdinalIgnoreCase) ? DateTime.Today.Month
                    : int.Parse(raw, CultureInfo.InvariantCulture) is var m and >= 1 and <= 12 ? m : throw new FormatException(),
                _ => int.Parse(raw, CultureInfo.InvariantCulture),
            };
        }
        catch (FormatException)
        {
            throw new ReportException($"'{p.Label}' has an invalid value.");
        }
    }
}
