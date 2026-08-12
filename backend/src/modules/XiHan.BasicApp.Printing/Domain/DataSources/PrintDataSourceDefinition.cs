// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.DataSources;

/// <summary>
/// 打印数据源定义：字段契约与静态样例数据的单一事实源，由业务模块在启动时注册。
/// </summary>
/// <param name="Code">数据源编码（模板 DataSourceCode 引用它，注册后不可变）</param>
/// <param name="Name">设计器展示名称</param>
/// <param name="Fields">字段清单（设计器素材与样例表单据此生成）</param>
/// <param name="SampleDataJson">静态样例数据 JSON（设计器预览与样例表单初值）</param>
public sealed record PrintDataSourceDefinition(
    string Code,
    string Name,
    IReadOnlyList<PrintDataSourceField> Fields,
    string SampleDataJson);

/// <summary>
/// 打印数据源字段定义。
/// </summary>
/// <param name="Key">数据字段路径（同一数据源内唯一）</param>
/// <param name="Label">设计器素材标题</param>
/// <param name="Kind">元素类型：text / image / barcode / qrcode / table</param>
/// <param name="Columns">明细表列（仅 Kind=table 时有值）</param>
/// <param name="InputType">样例表单控件类型：boolean / date / datetime / number / text / textarea（可空，按样例值推断）</param>
/// <param name="Placeholder">样例表单占位文案（可空）</param>
public sealed record PrintDataSourceField(
    string Key,
    string Label,
    string Kind = "text",
    IReadOnlyList<PrintDataSourceTableColumn>? Columns = null,
    string? InputType = null,
    string? Placeholder = null);

/// <summary>
/// 打印数据源明细表列定义。
/// </summary>
/// <param name="Field">数据字段路径</param>
/// <param name="Title">列标题</param>
/// <param name="Width">列宽（pt，可空）</param>
/// <param name="InputType">样例表格单元格控件类型（可空）</param>
public sealed record PrintDataSourceTableColumn(
    string Field,
    string Title,
    int? Width = null,
    string? InputType = null);
