using ClosedXML.Excel;
using Diagnostic.Report.Data;

namespace Diagnostic.Report.Documents;

/// <summary>Tabular report → .xlsx: title, filter line, styled header, data, and a total row for numeric columns.</summary>
public static class ExcelExport
{
    public static byte[] Build(TabularData data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName(data.Title));
        var cols = data.Columns.Count;

        ws.Cell(1, 1).Value = data.Title;
        ws.Range(1, 1, 1, Math.Max(cols, 1)).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        ws.Cell(2, 1).Value = data.Subtitle;
        ws.Range(2, 1, 2, Math.Max(cols, 1)).Merge().Style.Font.SetFontColor(XLColor.Gray);

        const int headerRow = 4;
        for (var c = 0; c < cols; c++) ws.Cell(headerRow, c + 1).Value = data.Columns[c];
        var header = ws.Range(headerRow, 1, headerRow, Math.Max(cols, 1));
        header.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B4F71"));

        var numeric = new bool[cols];
        for (var c = 0; c < cols; c++)
            numeric[c] = data.Rows.Count > 0 && data.Rows.All(r => r[c] is null or decimal or int or long or double or short or float)
                         && data.Rows.Any(r => r[c] is not null);

        var row = headerRow + 1;
        foreach (var r in data.Rows)
        {
            for (var c = 0; c < cols; c++)
            {
                var cell = ws.Cell(row, c + 1);
                switch (r[c])
                {
                    case null: break;
                    case DateTime d: cell.Value = d; cell.Style.DateFormat.Format = d.TimeOfDay == TimeSpan.Zero ? "dd-mmm-yyyy" : "dd-mmm-yyyy hh:mm"; break;
                    case decimal or double or float: cell.Value = Convert.ToDouble(r[c]); cell.Style.NumberFormat.Format = "#,##0.00"; break;
                    case int or long or short: cell.Value = Convert.ToInt64(r[c]); break;
                    default: cell.Value = r[c]!.ToString(); break;
                }
            }
            row++;
        }

        if (data.Rows.Count > 0 && numeric.Any(n => n))
        {
            ws.Cell(row, 1).Value = "Total";
            for (var c = 0; c < cols; c++)
                if (numeric[c] && !data.Columns[c].Contains('%'))
                {
                    var col = ws.Column(c + 1).ColumnLetter();
                    ws.Cell(row, c + 1).FormulaA1 = $"SUM({col}{headerRow + 1}:{col}{row - 1})";
                    ws.Cell(row, c + 1).Style.NumberFormat.Format = "#,##0.00";
                }
            ws.Range(row, 1, row, cols).Style.Font.SetBold().Border.SetTopBorder(XLBorderStyleValues.Thin);
        }

        if (cols > 0) ws.Range(headerRow, 1, Math.Max(row - 1, headerRow), cols).SetAutoFilter();
        ws.SheetView.FreezeRows(headerRow);
        ws.Columns().AdjustToContents(headerRow, row);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string SheetName(string title)
    {
        var name = new string(title.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
        return name.Length > 31 ? name[..31] : (name.Length == 0 ? "Report" : name);
    }
}
