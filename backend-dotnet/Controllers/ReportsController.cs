using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly KoperasiDbContext _context;

    public ReportsController(KoperasiDbContext context)
    {
        _context = context;
    }

    [HttpGet("stock-valuation")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetStockValuation()
    {
        var totalValueAtCost = await _context.Items.SumAsync(i => (i.Stock ?? 0) * (i.Cost ?? 0));
        var totalValueAtRetail = await _context.Items.SumAsync(i => (i.Stock ?? 0) * (i.Price ?? 0));
        var lowStockCount = await _context.Items
            .Where(i => (i.MinStock.HasValue && (i.Stock ?? 0) <= i.MinStock.Value) ||
                (!i.MinStock.HasValue && (i.Stock ?? 0) <= 10))
            .CountAsync();
        var potentialProfit = totalValueAtRetail - totalValueAtCost;

        return Ok(new
        {
            total_value_at_cost = totalValueAtCost,
            total_value_at_retail = totalValueAtRetail,
            potential_profit_if_all_sold = potentialProfit,
            low_stock_count = lowStockCount
        });
    }

    [HttpGet("summary")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        // The page sends date-only values (YYYY-MM-DD); treat `to` as an
        // inclusive day so rows from that whole day are counted.
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var paidTransactions = _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive && t.PaymentStatus == "paid");

        var revenue = await paidTransactions
            .SumAsync(t => (decimal)(t.TotalAmount ?? 0));

        var cost = await paidTransactions
            .SelectMany(t => t.Items!)
            .SumAsync(ti => (ti.Item != null ? (ti.Item.Cost ?? 0) : 0m) * ti.Quantity);

        var transactionCount = await paidTransactions
            .CountAsync();

        var discountGiven = await paidTransactions
            .SumAsync(t => (decimal)(t.DiscountAmount ?? 0));

        var taxCollected = await paidTransactions
            .SumAsync(t => (decimal)(t.TaxAmount ?? 0));

        var profit = revenue - cost;
        var profitMargin = revenue > 0 ? (profit / revenue * 100) : 0;
        var averageBasket = transactionCount > 0 ? (revenue / transactionCount) : 0;

        return Ok(new
        {
            revenue,
            cost_of_goods_sold = cost,
            profit = profit,
            profit_margin_pct = Math.Round(profitMargin, 2),
            discount_given = discountGiven,
            tax_collected = taxCollected,
            transaction_count = transactionCount,
            average_basket = Math.Round(averageBasket, 2),
            period = new
            {
                from = from?.ToString("yyyy-MM-dd"),
                to = to?.ToString("yyyy-MM-dd")
            }
        });
    }

    [HttpGet("best-sellers")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<IEnumerable<object>>> GetBestSellers(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 10)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var bestSellers = await _context.TransactionItems
            .Include(ti => ti.Item)
            .Where(ti => ti.Transaction.CreatedAt >= from && ti.Transaction.CreatedAt < toExclusive &&
                ti.Transaction.PaymentStatus == "paid")
            .GroupBy(ti => ti.Item != null ? ti.Item.Code : "unknown")
            .Select(g => new
            {
                name = g.FirstOrDefault()!.Item!.Name,
                sku = g.Key,
                units_sold = g.Sum(x => x.Quantity),
                revenue = g.Sum(x => (decimal)(x.Price ?? 0) * x.Quantity)
            })
            .OrderByDescending(x => x.revenue)
            .Take(Math.Clamp(limit, 1, 50))
            .ToListAsync();

        return Ok(bestSellers);
    }

    [HttpGet("daily-series")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<IEnumerable<object>>> GetDailySeries(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var rows = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive && t.PaymentStatus == "paid")
            .GroupBy(t => t.CreatedAt!.Value.Date)
            .Select(g => new
            {
                date = g.Key,
                revenue = g.Sum(t => (decimal)(t.TotalAmount ?? 0)),
                transactions = g.Count()
            })
            .OrderBy(x => x.date)
            .ToListAsync();

        return Ok(rows.Select(x => new
        {
            date = x.date.ToString("yyyy-MM-dd"),
            x.revenue,
            x.transactions
        }));
    }

    [HttpGet("transactions/export")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<IActionResult> ExportTransactions([FromQuery] string format = "csv", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var transactions = await _context.Transactions
            .Include(t => t.Member)
            .Include(t => t.User)
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item)
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var rows = transactions.Select(t => new ExportRow(
            t.TransactionNumber,
            t.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
            t.User?.Name ?? "-",
            t.Member?.Name ?? "-",
            t.Subtotal ?? 0,
            t.DiscountAmount ?? 0,
            t.TaxAmount ?? 0,
            t.TotalAmount ?? 0,
            t.PaymentMethod ?? "",
            t.PaymentStatus ?? ""
        )).ToList();

        var fileName = $"transactions_{from.Value:yyyy-MM-dd}_{to.Value:yyyy-MM-dd}.{format}";
        var period = $"{from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}";
        return format.ToLowerInvariant() switch
        {
            "csv" => File(Encoding.UTF8.GetBytes(BuildCsv(rows, period)), "text/csv", fileName),
            "xlsx" => File(BuildXlsx(rows, period), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName),
            "pdf" => File(BuildPdf(rows, period), "application/pdf", fileName),
            _ => BadRequest(new { message = "format must be csv, xlsx, or pdf" })
        };
    }

    private sealed record ExportRow(
        string Code,
        string CreatedAt,
        string Cashier,
        string Member,
        decimal Subtotal,
        decimal Discount,
        decimal Tax,
        decimal Total,
        string PaymentMethod,
        string Status);

    private static string BuildCsv(IEnumerable<ExportRow> rows, string period)
    {
        var csv = new StringBuilder();
        csv.Append('\uFEFF');
        csv.AppendLine("Transaction Report");
        csv.AppendLine($"Period,{EscapeCsv(period)}");
        csv.AppendLine();
        csv.AppendLine("Code,Date,Cashier,Member,Subtotal,Discount,Tax,Total,Payment Method,Status");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                row.Code, row.CreatedAt, row.Cashier, row.Member,
                row.Subtotal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Discount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Tax.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.PaymentMethod, row.Status
            }.Select(EscapeCsv)));
        }
        return csv.ToString();
    }

    private static string EscapeCsv(string value)
        => value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static byte[] BuildXlsx(IReadOnlyList<ExportRow> rows, string period)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddZipEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            AddZipEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AddZipEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Transactions" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);

            AddZipEntry(archive, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <numFmts count="1"><numFmt numFmtId="164" formatCode="#,##0.00"/></numFmts>
                  <fonts count="3">
                    <font><sz val="11"/><name val="Calibri"/></font>
                    <font><b/><sz val="16"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
                    <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
                  </fonts>
                  <fills count="4">
                    <fill><patternFill patternType="none"/></fill>
                    <fill><patternFill patternType="gray125"/></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF1B4D3E"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFE8F1ED"/><bgColor indexed="64"/></patternFill></fill>
                  </fills>
                  <borders count="2">
                    <border><left/><right/><top/><bottom/><diagonal/></border>
                    <border><left style="thin"/><right style="thin"/><top style="thin"/><bottom style="thin"/><diagonal/></border>
                  </borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="7">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
                    <xf numFmtId="0" fontId="1" fillId="2" borderId="0" applyFont="1" applyFill="1"/>
                    <xf numFmtId="0" fontId="0" fillId="3" borderId="0"/>
                    <xf numFmtId="0" fontId="2" fillId="2" borderId="1" applyFont="1" applyFill="1" applyBorder="1"/>
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="1" applyBorder="1"/>
                    <xf numFmtId="164" fontId="0" fillId="0" borderId="1" applyNumberFormat="1" applyBorder="1"/>
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right"/></xf>
                  </cellXfs>
                </styleSheet>
                """);

            var lastRow = rows.Count + 3;
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"3\" topLeftCell=\"A4\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><cols><col min=\"1\" max=\"1\" width=\"24\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"22\" customWidth=\"1\"/><col min=\"3\" max=\"4\" width=\"18\" customWidth=\"1\"/><col min=\"5\" max=\"8\" width=\"14\" customWidth=\"1\"/><col min=\"9\" max=\"10\" width=\"18\" customWidth=\"1\"/></cols><sheetData>");
            sheet.Append("<row r=\"1\" ht=\"26\" customHeight=\"1\"><c r=\"A1\" s=\"1\" t=\"inlineStr\"><is><t>Transaction Report</t></is></c></row>");
            sheet.Append($"<row r=\"2\"><c r=\"A2\" s=\"2\" t=\"inlineStr\"><is><t>{Xml($"Period: {period}")}</t></is></c></row>");
            sheet.Append("<row r=\"3\">");
            var headers = new[] { "Code", "Date", "Cashier", "Member", "Subtotal", "Discount", "Tax", "Total", "Payment Method", "Status" };
            for (var column = 0; column < headers.Length; column++)
                AppendInlineCell(sheet, $"{(char)('A' + column)}3", headers[column], 3);
            sheet.Append("</row>");
            for (var index = 0; index < rows.Count; index++)
            {
                var rowNumber = index + 4;
                var row = rows[index];
                sheet.Append($"<row r=\"{rowNumber}\">");
                AppendInlineCell(sheet, $"A{rowNumber}", row.Code, 4);
                AppendInlineCell(sheet, $"B{rowNumber}", row.CreatedAt, 4);
                AppendInlineCell(sheet, $"C{rowNumber}", row.Cashier, 4);
                AppendInlineCell(sheet, $"D{rowNumber}", row.Member, 4);
                AppendNumberCell(sheet, $"E{rowNumber}", row.Subtotal, 5);
                AppendNumberCell(sheet, $"F{rowNumber}", row.Discount, 5);
                AppendNumberCell(sheet, $"G{rowNumber}", row.Tax, 5);
                AppendNumberCell(sheet, $"H{rowNumber}", row.Total, 5);
                AppendInlineCell(sheet, $"I{rowNumber}", row.PaymentMethod, 4);
                AppendInlineCell(sheet, $"J{rowNumber}", row.Status, 4);
                sheet.Append("</row>");
            }
            sheet.Append("</sheetData><autoFilter ref=\"A3:J").Append(lastRow).Append("\"/><mergeCells count=\"1\"><mergeCell ref=\"A1:J1\"/></mergeCells></worksheet>");
            AddZipEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return stream.ToArray();
    }

    private static void AppendInlineCell(StringBuilder sheet, string reference, string value, int style)
        => sheet.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t>{Xml(value)}</t></is></c>");

    private static void AppendNumberCell(StringBuilder sheet, string reference, decimal value, int style)
        => sheet.Append($"<c r=\"{reference}\" s=\"{style}\"><v>{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>");

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static byte[] BuildPdf(IReadOnlyList<ExportRow> rows, string period)
    {
        const int rowsPerPage = 35;
        var pageRows = rows.Count == 0
            ? new[] { Array.Empty<ExportRow>() }
            : rows.Chunk(rowsPerPage).ToArray();

        var pageObjectNumbers = pageRows.Select((_, index) => 4 + index * 2).ToArray();
        var contentObjectNumbers = pageRows.Select((_, index) => 5 + index * 2).ToArray();
        var objectBodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pageRows.Length} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        for (var pageIndex = 0; pageIndex < pageRows.Length; pageIndex++)
        {
            var commands = BuildPdfPage(pageRows[pageIndex], period, pageIndex + 1, pageRows.Length);
            var pageObject = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumbers[pageIndex]} 0 R >>";
            var contentObject = $"<< /Length {Encoding.ASCII.GetByteCount(commands)} >>\nstream\n{commands}\nendstream";
            objectBodies.Add(pageObject);
            objectBodies.Add(contentObject);
        }

        var output = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objectBodies.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(index + 1).Append(" 0 obj\n").Append(objectBodies[index]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append("xref\n0 ").Append(objectBodies.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            output.Append(offset.ToString("D10")).Append(" 00000 n \n");
        output.Append("trailer\n<< /Size ").Append(objectBodies.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefOffset).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string BuildPdfPage(
        IReadOnlyList<ExportRow> rows, string period, int pageNumber, int pageCount)
    {
        var commands = new StringBuilder();
        commands.Append("q\n0.106 0.302 0.243 rg\n0 531 842 64 re\nf\nQ\n");
        PdfText(commands, "TRANSACTION REPORT", 36, 565, 18, "1 1 1 rg");
        PdfText(commands, period, 36, 545, 9, "0.85 0.95 0.90 rg");

        const int tableTop = 510;
        const int rowHeight = 12;
        const int left = 36;
        const int right = 806;
        commands.Append("0.91 0.95 0.93 rg\n");
        commands.Append(left).Append(' ').Append(tableTop - 4).Append(' ').Append(right - left).Append(" 18 re\nf\n");
        PdfText(commands, "CODE", 40, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "DATE", 100, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "CASHIER", 175, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "MEMBER", 250, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "SUBTOTAL", 380, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "DISC", 460, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "TAX", 510, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "TOTAL", 555, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "PAYMENT", 625, tableTop + 2, 8, "0.106 0.302 0.243 rg");
        PdfText(commands, "STATUS", 725, tableTop + 2, 8, "0.106 0.302 0.243 rg");

        var y = tableTop - 18;
        foreach (var row in rows)
        {
            if ((tableTop - y) / rowHeight % 2 == 0)
            {
                commands.Append("0.97 0.985 0.98 rg\n");
                commands.Append(left).Append(' ').Append(y - 4).Append(" 540 ").Append(rowHeight).Append(" re\nf\n");
            }

            PdfText(commands, Truncate(row.Code, 16), 40, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, Truncate(row.CreatedAt, 14), 100, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, Truncate(row.Cashier, 12), 175, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, Truncate(row.Member, 18), 250, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, $"Rp {row.Subtotal:N0}", 380, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, $"Rp {row.Discount:N0}", 460, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, $"Rp {row.Tax:N0}", 510, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, $"Rp {row.Total:N0}", 555, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, Truncate(row.PaymentMethod, 10), 625, y, 7.5, "0.12 0.15 0.14 rg");
            PdfText(commands, Truncate(row.Status, 10), 725, y, 7.5, "0.12 0.15 0.14 rg");
            y -= rowHeight;
        }

        commands.Append("0.75 0.82 0.78 RG\n0.5 w\n");
        commands.Append(left).Append(' ').Append(tableTop - 4).Append(" m ").Append(right).Append(' ').Append(tableTop - 4).Append(" l S\n");
        commands.Append(left).Append(' ').Append(y - 4).Append(" m ").Append(right).Append(' ').Append(y - 4).Append(" l S\n");
        PdfText(commands, $"{rows.Count} transaction(s)", 36, 30, 8, "0.35 0.42 0.38 rg");
        PdfText(commands, $"Page {pageNumber} of {pageCount}", 730, 30, 8, "0.35 0.42 0.38 rg");
        return commands.ToString();
    }

    private static void PdfText(StringBuilder commands, string text, double x, double y, double size, string color)
    {
        commands.Append(color).Append('\n');
        commands.Append("BT\n/F1 ").Append(size.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append(" Tf\n").Append(x.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(' ')
            .Append(y.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" Td\n(")
            .Append(Pdf(text)).Append(") Tj\nET\n");
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";

    private static string Xml(string value) => SecurityElement.Escape(value) ?? "";

    private static string Pdf(string value)
        => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
            .Select(c => c <= 127 ? c : '?').Aggregate(new StringBuilder(), (builder, c) => builder.Append(c)).ToString();

    [HttpGet("loan-report")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetLoanReport([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);
        var now = DateTime.UtcNow;

        var loans = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Where(l => l.CreatedAt >= from && l.CreatedAt < toExclusive)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        // RemainingAmount is EF-ignored (computed, not a DB column), so it is
        // derived here rather than trusting the property.
        decimal outstandingOf(Loan l) =>
            Math.Max((l.TotalAmount ?? ((l.PrincipalAmount ?? 0) + (l.InterestAmount ?? 0))) - (l.PaidAmount ?? 0), 0);

        var byStatus = loans
            .GroupBy(l => l.Status ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(new
        {
            total_loans = loans.Count,
            total_principal = loans.Sum(l => l.PrincipalAmount ?? 0),
            total_interest = loans.Sum(l => l.InterestAmount ?? 0),
            total_paid = loans.Sum(l => l.PaidAmount ?? 0),
            total_outstanding = loans.Sum(outstandingOf),
            overdue_count = loans.Count(l => l.Status == "active" && l.DueDate.HasValue && l.DueDate.Value < now),
            active_count = loans.Count(l => l.Status == "active"),
            pending_count = loans.Count(l => l.Status == "pending"),
            paid_off_count = loans.Count(l => l.Status == "paid_off"),
            by_status = byStatus,
            loans = loans.Select(l => new
            {
                id = l.Id,
                loan_code = l.LoanCode,
                member_id = l.MemberId,
                member_name = l.Member != null ? l.Member.Name : null,
                membership_id = l.Member != null ? l.Member.Identifier : null,
                product_name = l.LoanProduct != null ? l.LoanProduct.Name : null,
                principal = l.PrincipalAmount ?? 0,
                accrued_interest = l.InterestAmount ?? 0,
                interest_rate = l.InterestRate,
                term_months = l.TermInMonths,
                total_amount = l.TotalAmount ?? ((l.PrincipalAmount ?? 0) + (l.InterestAmount ?? 0)),
                total_paid = l.PaidAmount ?? 0,
                remaining_amount = outstandingOf(l),
                status = l.Status,
                applied_at = l.LoanDate,
                due_at = l.DueDate,
                paid_off_at = l.PaidAt
            })
        });
    }
}
