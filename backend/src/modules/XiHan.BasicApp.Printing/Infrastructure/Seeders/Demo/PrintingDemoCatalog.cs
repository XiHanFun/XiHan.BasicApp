// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Printing.Domain.DataSources;

namespace XiHan.BasicApp.Printing.Infrastructure.Seeders;

/// <summary>
/// 演示打印模板的登记项
/// </summary>
/// <param name="Code">模板编码</param>
/// <param name="Name">模板名称</param>
/// <param name="DataSourceCode">所绑数据源编码</param>
/// <param name="Remark">备注：对应的打印设计器文档站示例</param>
public sealed record PrintingDemoTemplate(string Code, string Name, string DataSourceCode, string Remark);

/// <summary>
/// 打印演示目录：五个演示数据源与六张演示模板
/// </summary>
/// <remarks>
/// 数据源（字段与样例数据）与模板（打印设计器的模板 JSON）都放在程序集资源 Demo/DataSources、Demo/Templates 下，
/// 前端用例读同一批文件核对模板能通过设计器的完整校验、按样例数据排版不出错误级诊断。
/// 五张模板对应打印设计器文档站的模板示例，「元素总览」摆出全部元素种类，绑内置数据源 system.print-demo。
/// </remarks>
public static class PrintingDemoCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 演示模板，按排序先后
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
    /// 演示数据源编码
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
    /// 读出演示数据源的定义
    /// </summary>
    /// <returns>五个演示数据源</returns>
    /// <exception cref="InvalidOperationException">资源缺失或内容不合法</exception>
    public static IReadOnlyList<PrintDataSourceDefinition> LoadDataSources()
    {
        return [.. DataSourceCodes.Select(static code =>
        {
            var file = JsonSerializer.Deserialize<DemoDataSourceFile>(ReadResource("DataSources", code), JsonOptions)
                ?? throw new InvalidOperationException($"打印演示数据源 {code} 的资源是 null。");
            if (!string.Equals(file.Code, code, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"打印演示数据源资源 {code} 里的编码是 {file.Code}。");
            }

            return new PrintDataSourceDefinition(
                file.Code,
                file.Name,
                [.. file.Fields.Select(static field => new PrintDataSourceField(
                    field.Key,
                    field.Label,
                    field.Kind ?? "text",
                    field.Columns?.Select(static column => new PrintDataSourceTableColumn(column.Field, column.Title, column.Width, column.InputType)).ToArray(),
                    field.InputType))],
                file.Sample.GetRawText());
        })];
    }

    /// <summary>
    /// 读出演示模板的模板 JSON
    /// </summary>
    /// <param name="code">模板编码</param>
    /// <returns>打印设计器的模板 JSON</returns>
    /// <exception cref="InvalidOperationException">资源缺失</exception>
    public static string LoadTemplateJson(string code)
    {
        return ReadResource("Templates", code);
    }

    private static string ReadResource(string folder, string code)
    {
        var assembly = typeof(PrintingDemoCatalog).Assembly;
        var suffix = $".Demo.{folder}.{code}.json";
        var name = assembly.GetManifestResourceNames().SingleOrDefault(resource => resource.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"缺少打印演示资源 {folder}/{code}.json。");
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed record DemoDataSourceFile(string Code, string Name, IReadOnlyList<DemoField> Fields, JsonElement Sample);

    private sealed record DemoField(string Key, string Label, string? Kind, IReadOnlyList<DemoColumn>? Columns, string? InputType);

    private sealed record DemoColumn(string Field, string Title, int? Width, string? InputType);
}
