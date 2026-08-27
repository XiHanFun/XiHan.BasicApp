// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 导出写出器测试（CSV / Xlsx）。
/// </summary>
/// <remarks>
/// 两个写出器都是零依赖手写序列化：CSV 的引号转义写错会让整列串行，
/// Xlsx 的 XML 转义写错会直接产出 Excel 打不开的损坏文件。
/// 这类问题在单元测试之外几乎只能靠人肉打开文件才能发现，所以这里逐条锁死转义规则、
/// 部件清单与进度回调节奏。
/// </remarks>
public sealed class SaasAppExportWriterTests
{
    /// <summary>
    /// CSV 必须以 UTF-8 BOM 开头，否则 Excel 打开中文会乱码。
    /// </summary>
    [Fact]
    public async Task Csv_ShouldStartWithUtf8Bom()
    {
        var bytes = await WriteCsvAsync([Column("name", "名称")], [["张三"]]);

        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    /// <summary>
    /// CSV 表头取列定义的 Title 且保持列顺序，行按 CRLF 断行。
    /// </summary>
    [Fact]
    public async Task Csv_ShouldWriteTitleHeaderAndCrlfRows()
    {
        var text = await WriteCsvTextAsync(
            [Column("name", "名称"), Column("age", "年龄")],
            [["张三", "18"], ["李四", "20"]]);

        Assert.Equal("名称,年龄\r\n张三,18\r\n李四,20\r\n", text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 无数据行时仍要写出表头，导出的空文件也应能被表格软件识别列结构。
    /// </summary>
    [Fact]
    public async Task Csv_WithoutRows_ShouldStillWriteHeader()
    {
        var text = await WriteCsvTextAsync([Column("name", "名称")], []);

        Assert.Equal("名称\r\n", text, StringComparer.Ordinal);
    }

    /// <summary>
    /// CSV 单元格转义：含逗号/引号/换行的值必须整体加引号，内部引号翻倍；其余原样输出。
    /// </summary>
    /// <param name="raw">原始单元格值。</param>
    /// <param name="expected">期望写出的单元格文本。</param>
    [Theory]
    [InlineData("普通", "普通")]
    [InlineData("", "")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("a\"b", "\"a\"\"b\"")]
    [InlineData("a\nb", "\"a\nb\"")]
    [InlineData("a\rb", "\"a\rb\"")]
    [InlineData("\"", "\"\"\"\"")]
    [InlineData("a,b\"c\nd", "\"a,b\"\"c\nd\"")]
    [InlineData(" 前后空白 ", " 前后空白 ")]
    [InlineData("制表\t符", "制表\t符")]
    public async Task Csv_Escape_ShouldFollowTheAgreedRules(string raw, string expected)
    {
        var text = await WriteCsvTextAsync([Column("c", "列")], [[raw]]);

        Assert.Equal($"列\r\n{expected}\r\n", text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 单元格为 null 时按空串写出，不得抛异常也不得写出 "null" 字面量。
    /// </summary>
    [Fact]
    public async Task Csv_NullCell_ShouldBeWrittenAsEmpty()
    {
        var text = await WriteCsvTextAsync([Column("a", "甲"), Column("b", "乙")], [[null!, "x"]]);

        Assert.Equal("甲,乙\r\n,x\r\n", text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 表头本身也走同一套转义规则（列标题里带逗号同样要加引号）。
    /// </summary>
    [Fact]
    public async Task Csv_HeaderTitle_ShouldBeEscapedToo()
    {
        var text = await WriteCsvTextAsync([Column("a", "姓名,别名")], []);

        Assert.Equal("\"姓名,别名\"\r\n", text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 输出流、列定义、行流三者均不接受 null。
    /// </summary>
    [Fact]
    public async Task Csv_NullArguments_ShouldThrow()
    {
        var writer = new CsvExportWriter();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => writer.WriteAsync(null!, [], AsyncRows([]), null));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => writer.WriteAsync(stream, null!, AsyncRows([]), null));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => writer.WriteAsync(stream, [], null!, null));
    }

    /// <summary>
    /// 进度回调：不足一个刷新批次时也必须在收尾时回调一次总行数（否则任务进度永远停在 0）。
    /// </summary>
    [Fact]
    public async Task Csv_OnProcessed_ShouldBeCalledOnceAtTheEndForSmallBatch()
    {
        var progress = new List<int>();
        await WriteCsvAsync([Column("c", "列")], [["1"], ["2"], ["3"]], processed => { progress.Add(processed); return Task.CompletedTask; });

        Assert.Equal([3], progress);
    }

    /// <summary>
    /// 零行导出同样要回调一次 0，让任务能正常收敛到完成态。
    /// </summary>
    [Fact]
    public async Task Csv_OnProcessed_ShouldReportZeroForEmptyExport()
    {
        var progress = new List<int>();
        await WriteCsvAsync([Column("c", "列")], [], processed => { progress.Add(processed); return Task.CompletedTask; });

        Assert.Equal([0], progress);
    }

    /// <summary>
    /// 每满 1000 行回调一次进度；正好 1000 行时批中回调与收尾回调各一次，都报 1000。
    /// </summary>
    [Fact]
    public async Task Csv_OnProcessed_ShouldFlushEveryThousandRows()
    {
        var rows = Enumerable.Range(0, 1000).Select(index => (IReadOnlyList<string>)[index.ToString()]).ToList();
        var progress = new List<int>();

        await WriteCsvAsync([Column("c", "列")], rows, processed => { progress.Add(processed); return Task.CompletedTask; });

        Assert.Equal([1000, 1000], progress);
    }

    /// <summary>
    /// 未提供进度回调时不得出错，导出照常完成。
    /// </summary>
    [Fact]
    public async Task Csv_WithoutOnProcessed_ShouldStillWriteAllRows()
    {
        var text = await WriteCsvTextAsync([Column("c", "列")], [["1"], ["2"]]);

        Assert.Equal("列\r\n1\r\n2\r\n", text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 取消令牌在行流推进时生效，导出应当中断而非跑完。
    /// </summary>
    [Fact]
    public async Task Csv_CancelledToken_ShouldStopWriting()
    {
        var writer = new CsvExportWriter();
        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => writer.WriteAsync(stream, [Column("c", "列")], CancellableRows([["1"]]), null, cts.Token));
    }

    /// <summary>
    /// 写出器自报的格式必须与实际产物一致，执行器据此挑选写出器。
    /// </summary>
    [Fact]
    public void Writers_ShouldReportTheirOwnFormat()
    {
        Assert.Equal(ExportFormat.Csv, new CsvExportWriter().Format);
        Assert.Equal(ExportFormat.Xlsx, new XlsxExportWriter().Format);
    }

    /// <summary>
    /// Xlsx 产物必须是完整 OOXML 包：五个固定部件一个不少，缺任何一个 Excel 都打不开。
    /// </summary>
    [Fact]
    public async Task Xlsx_ShouldContainAllRequiredOoxmlParts()
    {
        var bytes = await WriteXlsxAsync([Column("c", "列")], [["1"]]);

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();

        foreach (var required in new[]
        {
            "[Content_Types].xml",
            "_rels/.rels",
            "xl/workbook.xml",
            "xl/_rels/workbook.xml.rels",
            "xl/worksheets/sheet1.xml"
        })
        {
            Assert.True(entries.Contains(required, StringComparer.Ordinal), $"Xlsx 缺少部件 {required}，实际部件：{string.Join(", ", entries)}");
        }
    }

    /// <summary>
    /// 工作表第一行写表头，数据行从第 2 行开始，单元格一律 inlineStr（避免 Excel 猜错长数字类型）。
    /// </summary>
    [Fact]
    public async Task Xlsx_ShouldWriteHeaderAtRowOneAndDataFromRowTwo()
    {
        var sheet = await ReadXlsxSheetAsync([Column("a", "甲"), Column("b", "乙")], [["1", "2"], ["3", "4"]]);

        Assert.Contains("<row r=\"1\">", sheet, StringComparison.Ordinal);
        Assert.Contains("<c r=\"A1\" t=\"inlineStr\"><is><t xml:space=\"preserve\">甲</t></is></c>", sheet, StringComparison.Ordinal);
        Assert.Contains("<c r=\"B2\" t=\"inlineStr\"><is><t xml:space=\"preserve\">2</t></is></c>", sheet, StringComparison.Ordinal);
        Assert.Contains("<row r=\"3\">", sheet, StringComparison.Ordinal);
        Assert.EndsWith("</sheetData></worksheet>", sheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// 列序号转字母：第 26 列是 Z，第 27 列进位成 AA。
    /// </summary>
    [Fact]
    public async Task Xlsx_ColumnLetters_ShouldCarryOverAfterZ()
    {
        var columns = Enumerable.Range(0, 27).Select(index => Column($"k{index}", $"t{index}")).ToList();
        var row = Enumerable.Range(0, 27).Select(index => index.ToString()).ToList();

        var sheet = await ReadXlsxSheetAsync(columns, [row]);

        Assert.Contains("r=\"A1\"", sheet, StringComparison.Ordinal);
        Assert.Contains("r=\"Z1\"", sheet, StringComparison.Ordinal);
        Assert.Contains("r=\"AA1\"", sheet, StringComparison.Ordinal);
        Assert.Contains("r=\"AA2\"", sheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// XML 转义：&amp;、&lt;、&gt; 必须转义，否则生成的是损坏的表格。
    /// </summary>
    [Fact]
    public async Task Xlsx_ShouldEscapeXmlSpecialCharacters()
    {
        var sheet = await ReadXlsxSheetAsync([Column("c", "列")], [["a&b<c>d"]]);

        Assert.Contains("<t xml:space=\"preserve\">a&amp;b&lt;c&gt;d</t>", sheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// XML 1.0 非法控制字符必须被剔除，合法的制表/换行/回车保留。
    /// </summary>
    [Fact]
    public async Task Xlsx_ShouldDropIllegalControlCharactersButKeepTabAndNewline()
    {
        var sheet = await ReadXlsxSheetAsync([Column("c", "列")], [["a\u0001b\u001Fc\td\ne"]]);

        Assert.Contains("<t xml:space=\"preserve\">abc\td\ne</t>", sheet, StringComparison.Ordinal);
        Assert.DoesNotContain('\u0001', sheet);
        Assert.DoesNotContain('\u001F', sheet);
    }

    /// <summary>
    /// 单元格为 null 或空串时写出空文本节点，不抛异常。
    /// </summary>
    [Fact]
    public async Task Xlsx_NullOrEmptyCell_ShouldWriteEmptyText()
    {
        var sheet = await ReadXlsxSheetAsync([Column("a", "甲"), Column("b", "乙")], [[null!, ""]]);

        Assert.Contains("<c r=\"A2\" t=\"inlineStr\"><is><t xml:space=\"preserve\"></t></is></c>", sheet, StringComparison.Ordinal);
        Assert.Contains("<c r=\"B2\" t=\"inlineStr\"><is><t xml:space=\"preserve\"></t></is></c>", sheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// 行的单元格数少于列数时按实际单元格数写出（容错，不补空格也不越界）。
    /// </summary>
    [Fact]
    public async Task Xlsx_RowShorterThanColumns_ShouldWriteOnlyPresentCells()
    {
        var sheet = await ReadXlsxSheetAsync([Column("a", "甲"), Column("b", "乙"), Column("c", "丙")], [["only"]]);

        Assert.Contains("<c r=\"A2\" t=\"inlineStr\"><is><t xml:space=\"preserve\">only</t></is></c>", sheet, StringComparison.Ordinal);
        Assert.DoesNotContain("r=\"B2\"", sheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// 行的单元格数超出列数时，多出的单元格不带 r 引用而不是越界崩溃。
    /// </summary>
    [Fact]
    public async Task Xlsx_RowLongerThanColumns_ShouldEmitExtraCellsWithoutReference()
    {
        var sheet = await ReadXlsxSheetAsync([Column("a", "甲")], [["x", "y"]]);

        Assert.Contains("<c r=\"A2\" t=\"inlineStr\"><is><t xml:space=\"preserve\">x</t></is></c>", sheet, StringComparison.Ordinal);
        Assert.Contains("<c t=\"inlineStr\"><is><t xml:space=\"preserve\">y</t></is></c>", sheet, StringComparison.Ordinal);
    }

    /// <summary>
    /// Xlsx 的进度回调节奏与 CSV 一致：收尾必回调一次总行数。
    /// </summary>
    [Fact]
    public async Task Xlsx_OnProcessed_ShouldReportFinalCount()
    {
        var progress = new List<int>();
        await WriteXlsxAsync([Column("c", "列")], [["1"], ["2"]], processed => { progress.Add(processed); return Task.CompletedTask; });

        Assert.Equal([2], progress);
    }

    /// <summary>
    /// Xlsx 写出器同样拒绝 null 入参。
    /// </summary>
    [Fact]
    public async Task Xlsx_NullArguments_ShouldThrow()
    {
        var writer = new XlsxExportWriter();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => writer.WriteAsync(null!, [], AsyncRows([]), null));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => writer.WriteAsync(stream, null!, AsyncRows([]), null));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => writer.WriteAsync(stream, [], null!, null));
    }

    /// <summary>
    /// 导出默认参数是安全上限，改动即意味着运行特征变化，锁死取值。
    /// </summary>
    [Fact]
    public void ExportDefaults_ShouldKeepAgreedLimits()
    {
        Assert.Equal(1000, ExportDefaults.BatchSize);
        Assert.Equal(1_000_000, ExportDefaults.MaxRows);
        Assert.Equal(1000, ExportDefaults.CurrentPageMaxSize);
    }

    /// <summary>
    /// 构造一个导出列定义。
    /// </summary>
    /// <param name="key">字段键。</param>
    /// <param name="title">列标题。</param>
    /// <returns>列定义。</returns>
    private static ExportColumnDto Column(string key, string title)
    {
        return new ExportColumnDto { Key = key, Title = title };
    }

    /// <summary>
    /// 把同步行集合包装成行流。
    /// </summary>
    /// <param name="rows">行集合。</param>
    /// <returns>异步行流。</returns>
    private static async IAsyncEnumerable<IReadOnlyList<string>> AsyncRows(IEnumerable<IReadOnlyList<string>> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
            await Task.Yield();
        }
    }

    /// <summary>
    /// 把同步行集合包装成**会响应取消**的行流。
    /// </summary>
    /// <param name="rows">行集合。</param>
    /// <param name="cancellationToken">由 WithCancellation 注入的取消令牌。</param>
    /// <returns>异步行流。</returns>
    private static async IAsyncEnumerable<IReadOnlyList<string>> CancellableRows(
        IEnumerable<IReadOnlyList<string>> rows,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }

    /// <summary>
    /// 用 CSV 写出器产出字节。
    /// </summary>
    /// <param name="columns">列定义。</param>
    /// <param name="rows">行集合。</param>
    /// <param name="onProcessed">进度回调。</param>
    /// <returns>产物字节。</returns>
    private static async Task<byte[]> WriteCsvAsync(
        IReadOnlyList<ExportColumnDto> columns,
        IEnumerable<IReadOnlyList<string>> rows,
        Func<int, Task>? onProcessed = null)
    {
        using var stream = new MemoryStream();
        await new CsvExportWriter().WriteAsync(stream, columns, AsyncRows(rows), onProcessed);
        return stream.ToArray();
    }

    /// <summary>
    /// 用 CSV 写出器产出文本（已去掉 BOM）。
    /// </summary>
    /// <param name="columns">列定义。</param>
    /// <param name="rows">行集合。</param>
    /// <returns>去 BOM 后的文本。</returns>
    private static async Task<string> WriteCsvTextAsync(
        IReadOnlyList<ExportColumnDto> columns,
        IEnumerable<IReadOnlyList<string>> rows)
    {
        var bytes = await WriteCsvAsync(columns, rows);
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    /// <summary>
    /// 用 Xlsx 写出器产出字节。
    /// </summary>
    /// <param name="columns">列定义。</param>
    /// <param name="rows">行集合。</param>
    /// <param name="onProcessed">进度回调。</param>
    /// <returns>产物字节。</returns>
    private static async Task<byte[]> WriteXlsxAsync(
        IReadOnlyList<ExportColumnDto> columns,
        IEnumerable<IReadOnlyList<string>> rows,
        Func<int, Task>? onProcessed = null)
    {
        using var stream = new MemoryStream();
        await new XlsxExportWriter().WriteAsync(stream, columns, AsyncRows(rows), onProcessed);
        return stream.ToArray();
    }

    /// <summary>
    /// 产出 Xlsx 并读回工作表 XML 文本。
    /// </summary>
    /// <param name="columns">列定义。</param>
    /// <param name="rows">行集合。</param>
    /// <returns>sheet1.xml 文本。</returns>
    private static async Task<string> ReadXlsxSheetAsync(
        IReadOnlyList<ExportColumnDto> columns,
        IEnumerable<IReadOnlyList<string>> rows)
    {
        var bytes = await WriteXlsxAsync(columns, rows);
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("Xlsx 产物缺少 sheet1.xml。");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
