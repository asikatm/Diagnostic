using Diagnostic.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Services;

/// <summary>
/// Generates gap-safe running numbers (INV2609-00001 style) from admin.NumberSequences using a row lock,
/// so two counters never hand out the same number. Runs inside the caller's transaction when there is one.
/// </summary>
public class NumberSequenceService(DiagnosticDbContext db)
{
    public const string Patient = "PATIENT";
    public const string Invoice = "INVOICE";
    public const string Receipt = "RECEIPT";
    public const string Sample = "SAMPLE";

    private static readonly Dictionary<string, (string Prefix, bool Yearly)> Defaults = new()
    {
        [Patient] = ("P", true),
        [Invoice] = ("INV", false),
        [Receipt] = ("MR", false),
        [Sample] = ("S", false),
    };

    private sealed class SeqRow
    {
        public string Prefix { get; set; } = "";
        public int LastNumber { get; set; }
        public int Padding { get; set; }
    }

    public async Task<string> NextAsync(string key)
    {
        var yearly = Defaults.TryGetValue(key, out var d) && d.Yearly;
        var period = DateTime.Now.ToString(yearly ? "yy" : "yyMM");

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var row = (await db.Database.SqlQuery<SeqRow>($"""
                UPDATE admin.NumberSequences WITH (ROWLOCK, UPDLOCK)
                SET LastNumber = CASE WHEN Period = {period} THEN LastNumber + 1 ELSE 1 END,
                    Period = {period}
                OUTPUT inserted.Prefix, inserted.LastNumber, inserted.Padding
                WHERE SequenceKey = {key}
                """).ToListAsync()).FirstOrDefault();

            if (row != null) return $"{row.Prefix}{period}{row.LastNumber.ToString().PadLeft(row.Padding, '0')}";

            // First use of this key – create the counter row.
            var prefix = d.Prefix ?? key[..3];
            await db.Database.ExecuteSqlAsync($"""
                IF NOT EXISTS (SELECT 1 FROM admin.NumberSequences WHERE SequenceKey = {key})
                INSERT INTO admin.NumberSequences (SequenceKey, Prefix, Period, LastNumber, Padding)
                VALUES ({key}, {prefix}, {period}, 0, 5)
                """);
        }
        throw new InvalidOperationException($"Could not generate number for '{key}'.");
    }
}
