// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Printing;

/// <summary>
/// 打印数据源的定义。
/// </summary>
/// <remarks>
/// 字段契约与静态样例数据，业务包在 <c>Configure</c> 里经 <see cref="PrintDataSourceServiceCollectionExtensions.AddPrintDataSource"/> 登记（照 4.x 的 <c>RegisterPrintDataSource</c>）。
/// 只是元数据：后端不取业务数据，真实打印数据由前端调用方传给打印服务；模板的数据源编码引用它。
/// 编码、字段种类与样例控件类型由 Printing 包在启动时校验，非法或重复即启动失败；Printing 关掉时没有消费方，登记照样无害。
/// </remarks>
/// <param name="Code">编码：不超过 100 个字符、不含空白，登记后不变。</param>
/// <param name="Name">设计器里显示的名称。</param>
/// <param name="Fields">字段清单：至少一个，键在本数据源里唯一。</param>
/// <param name="SampleDataJson">静态样例数据：根是对象或数组的 JSON。</param>
public sealed record PrintDataSourceDefinition(string Code, string Name, IReadOnlyList<PrintDataSourceField> Fields, string SampleDataJson);

/// <summary>
/// 打印数据源的一个字段。
/// </summary>
/// <param name="Key">数据字段路径：不超过 100 个字符、不含空白，同一数据源里唯一。</param>
/// <param name="Label">设计器素材的标题。</param>
/// <param name="Kind">元素种类：<c>text</c>、<c>image</c>、<c>barcode</c>、<c>qrcode</c>、<c>table</c>。</param>
/// <param name="Columns">明细表的列：只在 <c>table</c> 时给，且至少一列。</param>
/// <param name="InputType">样例表单的控件：<c>boolean</c>、<c>date</c>、<c>datetime</c>、<c>number</c>、<c>text</c>、<c>textarea</c>；不给即按样例值推断。</param>
/// <param name="Placeholder">样例表单的占位文案。</param>
public sealed record PrintDataSourceField(
    string Key,
    string Label,
    string Kind = PrintFieldKinds.Text,
    IReadOnlyList<PrintDataSourceTableColumn>? Columns = null,
    string? InputType = null,
    string? Placeholder = null);

/// <summary>
/// 打印数据源明细表的一列。
/// </summary>
/// <param name="Field">数据字段路径：不含空白，同一明细表里唯一。</param>
/// <param name="Title">列标题。</param>
/// <param name="Width">列宽（pt）。</param>
/// <param name="InputType">样例表格单元格的控件，取值同字段的 <c>InputType</c>。</param>
/// <param name="Placeholder">样例表格单元格的占位文案。</param>
public sealed record PrintDataSourceTableColumn(string Field, string Title, int? Width = null, string? InputType = null, string? Placeholder = null);

/// <summary>
/// 打印字段的元素种类。
/// </summary>
public static class PrintFieldKinds
{
    /// <summary>
    /// 文本。
    /// </summary>
    public const string Text = "text";

    /// <summary>
    /// 图片。
    /// </summary>
    public const string Image = "image";

    /// <summary>
    /// 条码。
    /// </summary>
    public const string Barcode = "barcode";

    /// <summary>
    /// 二维码。
    /// </summary>
    public const string QrCode = "qrcode";

    /// <summary>
    /// 明细表。
    /// </summary>
    public const string Table = "table";
}

/// <summary>
/// 一条打印数据源的登记。
/// </summary>
/// <remarks>
/// 以单例投进容器，Printing 的注册表在启动时统一收下并校验。
/// </remarks>
/// <param name="Definition">数据源定义。</param>
public sealed record PrintDataSourceRegistration(PrintDataSourceDefinition Definition);

/// <summary>
/// 打印数据源的登记。
/// </summary>
public static class PrintDataSourceServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个打印数据源。
    /// </summary>
    /// <remarks>
    /// 重复的编码与非法的字段契约在 Printing 启动时报出，不在这里判。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="definition">数据源定义。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddPrintDataSource(this IServiceCollection services, PrintDataSourceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(definition);
        return services.AddSingleton(new PrintDataSourceRegistration(definition));
    }
}
