using ClosedXML.Excel;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;

namespace FoodOps.Infrastructure.Services;

public class ExcelReportExporter : IReportExporter
{
    public byte[] RevenueToExcel(string period, IReadOnlyList<RevenuePointDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Revenue");

        ws.Cell(1, 1).Value = "Period";
        ws.Cell(1, 2).Value = "Orders";
        ws.Cell(1, 3).Value = "Revenue (INR)";
        ws.Range(1, 1, 1, 3).Style.Font.Bold = true;

        var r = 2;
        foreach (var p in rows)
        {
            ws.Cell(r, 1).Value = p.Label;
            ws.Cell(r, 2).Value = p.Orders;
            ws.Cell(r, 3).Value = p.Revenue;
            r++;
        }

        // total row uses real Excel formulas, so the sheet stays correct if someone edits it
        ws.Cell(r, 1).Value = "Total";
        ws.Cell(r, 2).FormulaA1 = $"SUM(B2:B{r - 1})";
        ws.Cell(r, 3).FormulaA1 = $"SUM(C2:C{r - 1})";
        ws.Range(r, 1, r, 3).Style.Font.Bold = true;

        ws.Column(3).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}