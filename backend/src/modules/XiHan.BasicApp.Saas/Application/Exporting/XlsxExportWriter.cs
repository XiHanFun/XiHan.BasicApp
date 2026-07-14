#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XlsxExportWriter
// Guid:f1e2d3c4-b5a6-4978-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.IO.Compression;
using System.Text;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// Xlsx（Excel）写出器。
/// </summary>
/// <remarks>
/// 零第三方依赖：直接把 OOXML（SpreadsheetML）各部件写进一个 ZIP 包。
/// 工作表 XML 流式写入 zip 条目、单元格统一用内联字符串（inlineStr），
/// 不需要共享字符串表缓冲，因此内存占用恒定、可支撑大行数导出。
/// 所有单元格按字符串写出（与 CSV 写出器一致），避免 Excel 把长数字/前导零猜错类型。
/// </remarks>
public sealed class XlsxExportWriter : IExportWriter
{
    private const int ProgressFlushInterval = 1000;

    private const string ContentTypesXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "</Types>";

    private const string RootRelsXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private const string WorkbookXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
        "</workbook>";

    private const string WorkbookRelsXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "</Relationships>";

    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Xlsx;

    /// <inheritdoc />
    public async Task WriteAsync(
        Stream output,
        IReadOnlyList<ExportColumnDto> columns,
        IAsyncEnumerable<IReadOnlyList<string>> rows,
        Func<int, Task>? onProcessed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        // 预计算列字母（A、B……AA），避免每格重复计算
        var columnLetters = new string[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            columnLetters[i] = ColumnLetter(i + 1);
        }

        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);

        // 固定部件（逐个开-写-关，Create 模式同一时刻只允许一个条目流打开）
        await WriteTextEntryAsync(archive, "[Content_Types].xml", ContentTypesXml, cancellationToken);
        await WriteTextEntryAsync(archive, "_rels/.rels", RootRelsXml, cancellationToken);
        await WriteTextEntryAsync(archive, "xl/workbook.xml", WorkbookXml, cancellationToken);
        await WriteTextEntryAsync(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml, cancellationToken);

        // 工作表：流式写入
        var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
        await using var entryStream = sheetEntry.Open();
        await using var writer = new StreamWriter(entryStream, new UTF8Encoding(false), 1 << 16);

        await writer.WriteAsync(
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        // 表头（第 1 行）
        var line = new StringBuilder(256);
        AppendRow(line, 1, columnLetters, columns.Select(column => column.Title));
        await writer.WriteAsync(line.ToString());

        var processed = 0;
        var rowIndex = 2;
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            line.Clear();
            AppendRow(line, rowIndex, columnLetters, row);
            await writer.WriteAsync(line.ToString());

            processed++;
            rowIndex++;

            if (processed % ProgressFlushInterval == 0 && onProcessed is not null)
            {
                await writer.FlushAsync(cancellationToken);
                await onProcessed(processed);
            }
        }

        await writer.WriteAsync("</sheetData></worksheet>");
        await writer.FlushAsync(cancellationToken);

        if (onProcessed is not null)
        {
            await onProcessed(processed);
        }
    }

    private static void AppendRow(StringBuilder builder, int rowIndex, string[] columnLetters, IEnumerable<string> cells)
    {
        builder.Append("<row r=\"").Append(rowIndex).Append("\">");

        var col = 0;
        foreach (var cell in cells)
        {
            // 行的单元格数可能少于列数（容错），超出预算列字母时跳过引用
            var reference = col < columnLetters.Length ? columnLetters[col] + rowIndex : null;
            builder.Append("<c");
            if (reference is not null)
            {
                builder.Append(" r=\"").Append(reference).Append('"');
            }
            builder.Append(" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                   .Append(EscapeXml(cell))
                   .Append("</t></is></c>");
            col++;
        }

        builder.Append("</row>");
    }

    private static async Task WriteTextEntryAsync(ZipArchive archive, string entryName, string content, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// 列序号转字母（1→A、26→Z、27→AA）
    /// </summary>
    private static string ColumnLetter(int index)
    {
        var builder = new StringBuilder();
        while (index > 0)
        {
            var remainder = (index - 1) % 26;
            builder.Insert(0, (char)('A' + remainder));
            index = (index - 1) / 26;
        }
        return builder.ToString();
    }

    /// <summary>
    /// XML 元素内容转义，并剔除 XML 1.0 非法控制字符
    /// </summary>
    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            // XML 1.0 合法控制字符仅 \t \n \r，其余低位控制字符剔除，避免生成损坏的表格
            if (ch < 0x20 && ch is not '\t' and not '\n' and not '\r')
            {
                continue;
            }

            switch (ch)
            {
                case '&':
                    builder.Append("&amp;");
                    break;

                case '<':
                    builder.Append("&lt;");
                    break;

                case '>':
                    builder.Append("&gt;");
                    break;

                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }
}
