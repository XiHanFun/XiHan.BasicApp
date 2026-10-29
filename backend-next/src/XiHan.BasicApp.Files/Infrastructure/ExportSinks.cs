// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Security;
using System.Text;
using XiHan.BasicApp.Files.Domain.Entities;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 导出文件的写入端。
/// </summary>
/// <remarks>
/// 先写表头，再一行一行写，最后收尾；内容直接写进调用方给的流（导出时是临时文件），不在内存里攒整份结果。
/// </remarks>
public interface IExportSink : IAsyncDisposable
{
    /// <summary>
    /// 写一行（表头也是一行）。
    /// </summary>
    /// <param name="cells">单元格文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteRowAsync(IReadOnlyList<string> cells, CancellationToken cancellationToken);

    /// <summary>
    /// 收尾（补齐文件结构）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task CompleteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// 导出写入端的工厂。
/// </summary>
public static class ExportSinks
{
    /// <summary>
    /// 按格式建写入端。
    /// </summary>
    /// <param name="format">格式。</param>
    /// <param name="output">输出流（写入端不释放它）。</param>
    /// <returns>写入端。</returns>
    public static IExportSink Create(ExportFormat format, Stream output)
        => format == ExportFormat.Xlsx ? new XlsxExportSink(output) : new CsvExportSink(output);

    /// <summary>
    /// 结果文件的扩展名。
    /// </summary>
    /// <param name="format">格式。</param>
    /// <returns>扩展名（含点）。</returns>
    public static string ExtensionOf(ExportFormat format) => format == ExportFormat.Xlsx ? ".xlsx" : ".csv";

    /// <summary>
    /// 结果文件的内容类型。
    /// </summary>
    /// <param name="format">格式。</param>
    /// <returns>内容类型。</returns>
    public static string ContentTypeOf(ExportFormat format)
        => format == ExportFormat.Xlsx ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv";
}

/// <summary>
/// CSV 写入端。
/// </summary>
/// <remarks>
/// 4.x 同：UTF-8 带 BOM（Excel 直接打开不乱码），CRLF 换行，含逗号、引号或换行的单元格加引号、引号成对转义。
/// </remarks>
/// <param name="output">输出流。</param>
internal sealed class CsvExportSink(Stream output) : IExportSink
{
    private readonly StreamWriter _writer = new(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), bufferSize: 65536, leaveOpen: true) { NewLine = "\r\n" };

    /// <inheritdoc />
    public async Task WriteRowAsync(IReadOnlyList<string> cells, CancellationToken cancellationToken)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                await _writer.WriteAsync(',').ConfigureAwait(false);
            }

            await _writer.WriteAsync(Escape(cells[i]).AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        await _writer.WriteLineAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task CompleteAsync(CancellationToken cancellationToken) => _writer.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _writer.DisposeAsync();

    private static string Escape(string value)
        => value.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}

/// <summary>
/// XLSX 写入端。
/// </summary>
/// <remarks>
/// 4.x 同：一个工作表、内联字符串，直接写 OOXML 的五个部件；工作表部件最后写，行随到随写。
/// </remarks>
internal sealed class XlsxExportSink : IExportSink
{
    private readonly ZipArchive _archive;
    private readonly Stream _sheet;
    private readonly StreamWriter _writer;
    private int _row;

    public XlsxExportSink(Stream output)
    {
        _archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        Part("[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
        Part("_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
        Part("xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Sheet1" sheetId="1" r:id="rId1"/></sheets></workbook>""");
        Part("xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
        _sheet = _archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal).Open();
        _writer = new StreamWriter(_sheet, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 65536);
        _writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
    }

    /// <inheritdoc />
    public async Task WriteRowAsync(IReadOnlyList<string> cells, CancellationToken cancellationToken)
    {
        _row++;
        var builder = new StringBuilder("<row r=\"").Append(_row).Append("\">");
        foreach (var cell in cells)
        {
            builder.Append("<c t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(SecurityElement.Escape(Clean(cell))).Append("</t></is></c>");
        }

        builder.Append("</row>");
        await _writer.WriteAsync(builder, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await _writer.WriteAsync("</sheetData></worksheet>".AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
        await _sheet.DisposeAsync().ConfigureAwait(false);
        _archive.Dispose();
    }

    /// <summary>
    /// XML 1.0 不允许的控制字符去掉（制表、换行、回车保留）。
    /// </summary>
    private static string Clean(string value)
        => value.Any(static c => c < 0x20 && c is not ('\t' or '\n' or '\r')) ? new string([.. value.Where(static c => c >= 0x20 || c is '\t' or '\n' or '\r')]) : value;

    private void Part(string name, string content)
    {
        using var writer = new StreamWriter(_archive.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }
}
