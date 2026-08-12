// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Application.Dtos;

/// <summary>
/// 打印数据源目录项。
/// </summary>
public sealed class PrintDataSourceDto
{
    /// <summary>数据源编码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>设计器展示名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>字段清单。</summary>
    public List<PrintDataSourceFieldDto> Fields { get; set; } = [];

    /// <summary>静态样例数据 JSON。</summary>
    public string SampleDataJson { get; set; } = string.Empty;
}

/// <summary>
/// 打印数据源字段。
/// </summary>
public sealed class PrintDataSourceFieldDto
{
    /// <summary>数据字段路径。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>设计器素材标题。</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>元素类型：text / image / barcode / qrcode / table。</summary>
    public string Kind { get; set; } = "text";

    /// <summary>明细表列（仅 table 类型）。</summary>
    public List<PrintDataSourceTableColumnDto>? Columns { get; set; }

    /// <summary>样例表单控件类型（可空）。</summary>
    public string? InputType { get; set; }

    /// <summary>样例表单占位文案（可空）。</summary>
    public string? Placeholder { get; set; }
}

/// <summary>
/// 打印数据源明细表列。
/// </summary>
public sealed class PrintDataSourceTableColumnDto
{
    /// <summary>数据字段路径。</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>列标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>列宽（pt，可空）。</summary>
    public int? Width { get; set; }

    /// <summary>样例表格单元格控件类型（可空）。</summary>
    public string? InputType { get; set; }
}
