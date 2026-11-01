// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Printing;
using XiHan.BasicApp.Printing.Domain.Services;

namespace XiHan.BasicApp.Printing.Seeds;

/// <summary>
/// 一张演示模板的登记。
/// </summary>
/// <param name="Code">模板编码。</param>
/// <param name="Name">模板名称。</param>
/// <param name="DataSourceCode">所绑数据源的编码。</param>
/// <param name="Remark">备注：对应的打印设计器文档站示例。</param>
public sealed record PrintingDemoTemplate(string Code, string Name, string DataSourceCode, string Remark);

/// <summary>
/// 打印演示目录：五个演示数据源与六张演示模板。
/// </summary>
/// <remarks>
/// 数据源（字段与样例）与模板（打印设计器的模板 JSON）是嵌入的 <c>Seeds/Demo/DataSources</c>、<c>Seeds/Demo/Templates</c>，与 4.x 同一批文件原样搬入；
/// 前端用例读同一批文件核对每张模板能通过设计器的完整校验、按样例排版不出错误级诊断。五张模板对应打印设计器文档站的示例，「元素总览」摆出全部元素种类、绑内置数据源 <c>system.print-demo</c>。
/// 演示数据源只在演示开关打开时登记（4.x 同），演示模板由 <see cref="DemoPrintTemplateSeeder"/> 写入。
/// </remarks>
public static class PrintingDemoCatalog
{
    /// <summary>
    /// 演示模板，按排序先后。
    /// </summary>
    public static IReadOnlyList<PrintingDemoTemplate> Templates { get; } =
    [
        new("demo.pickup-receipt", "取件凭条", "demo.pickup-receipt", "演示数据：最小模板，模板文字与放不下时缩小字号"),
        new("demo.delivery-note", "送货单", "demo.delivery-note", "演示数据：页眉标题、页码、字段与日期格式、明细表跨页、序号列与合计行"),
        new("demo.formats", "格式示例", "demo.formats", "演示数据：数字、百分比、日期、人民币大写、中文数字、值映射、表达式、空值与占位模板"),
        new("demo.inspection", "设备巡检记录", "demo.inspection", "演示数据：首页页眉、长文本、表头每页重复、本页计数与数据占位水印"),
        new("demo.outage-notice", "停电检修通知", "demo.outage-notice", "演示数据：静态富文本与行内字段，数据里的 HTML 排成富文本（脚本被丢弃）"),
        new("demo.element-overview", "打印元素总览", BuiltInPrintDataSources.SystemPrintDemo.Code, "演示数据：全部元素种类、七种条码与四种二维码、图形、表格、页眉页脚变体、续排页码与水印"),
    ];

    /// <summary>
    /// 演示数据源的编码。
    /// </summary>
    public static IReadOnlyList<string> DataSourceCodes { get; } =
    [
        "demo.pickup-receipt",
        "demo.delivery-note",
        "demo.formats",
        "demo.inspection",
        "demo.outage-notice",
    ];

    /// <summary>
    /// 读出演示数据源的定义。
    /// </summary>
    /// <returns>五个演示数据源。</returns>
    /// <exception cref="InvalidOperationException">资源缺失或与编码不符。</exception>
    public static IReadOnlyList<PrintDataSourceDefinition> LoadDataSources()
        => [.. DataSourceCodes.Select(static code => ParseDataSource(code, ReadResource("DataSources", code)))];

    /// <summary>
    /// 读出演示模板的模板 JSON。
    /// </summary>
    /// <param name="code">模板编码。</param>
    /// <returns>打印设计器的模板 JSON。</returns>
    /// <exception cref="InvalidOperationException">资源缺失。</exception>
    public static string LoadTemplateJson(string code) => ReadResource("Templates", code);

    private static PrintDataSourceDefinition ParseDataSource(string code, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var declared = root.GetProperty("code").GetString();
        if (!string.Equals(declared, code, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Print demo data source resource '{code}' declares code '{declared}'.");
        }

        var fields = root.GetProperty("fields").EnumerateArray().Select(static field => new PrintDataSourceField(
            field.GetProperty("key").GetString()!,
            field.GetProperty("label").GetString()!,
            Text(field, "kind") ?? PrintFieldKinds.Text,
            field.TryGetProperty("columns", out var columns)
                ? [.. columns.EnumerateArray().Select(static column => new PrintDataSourceTableColumn(
                    column.GetProperty("field").GetString()!,
                    column.GetProperty("title").GetString()!,
                    column.TryGetProperty("width", out var width) ? width.GetInt32() : (int?)null,
                    Text(column, "inputType")))]
                : null,
            Text(field, "inputType"))).ToList();
        return new PrintDataSourceDefinition(code, root.GetProperty("name").GetString()!, fields, root.GetProperty("sample").GetRawText());
    }

    private static string? Text(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) ? value.GetString() : null;

    private static string ReadResource(string folder, string code)
    {
        var name = $"Demo/{folder}/{code}.json";
        using var stream = typeof(PrintingDemoCatalog).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Print demo resource '{name}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
