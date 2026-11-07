// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Printing;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Printing.Operations.PrintDataSources;

/// <summary>
/// 打印数据源目录
/// </summary>
/// <remarks>
/// <c>GET /api/print-data-sources</c>。全部已登记的数据源（含字段与样例 JSON），按编码排序；设计器的数据源下拉、字段素材与样例数据都取这一份
/// 持有模板的读取或使用权限其一即可（<see cref="PrintingPolicies.DataSourceCatalog"/>，照 4.x）
/// </remarks>
/// <param name="registry">数据源注册表</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = PrintingPolicies.DataSourceCatalog)]
public sealed class ListPrintDataSources(PrintDataSourceRegistry registry) : IOperation<ListPrintDataSourcesRequest, PrintDataSourcesResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-data-sources.list";

    /// <inheritdoc />
    public ValueTask<Result<PrintDataSourcesResponse>> HandleAsync(ListPrintDataSourcesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.FromResult(Result.Success(new PrintDataSourcesResponse([.. registry.All.Select(PrintDataSourceResponse.From)])));
    }
}

/// <summary>
/// <c>print-data-sources.list</c> 的请求：没有参数
/// </summary>
public sealed record ListPrintDataSourcesRequest;

/// <summary>
/// 打印数据源目录
/// </summary>
/// <param name="Items">数据源，按编码排序</param>
public sealed record PrintDataSourcesResponse(IReadOnlyList<PrintDataSourceResponse> Items);

/// <summary>
/// 一个打印数据源
/// </summary>
/// <param name="Code">编码</param>
/// <param name="Name">设计器里显示的名称</param>
/// <param name="Fields">字段清单</param>
/// <param name="SampleDataJson">静态样例数据 JSON</param>
public sealed record PrintDataSourceResponse(string Code, string Name, IReadOnlyList<PrintDataSourceFieldResponse> Fields, string SampleDataJson)
{
    /// <summary>
    /// 从登记的定义投影
    /// </summary>
    /// <param name="definition">数据源定义</param>
    public static PrintDataSourceResponse From(PrintDataSourceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new PrintDataSourceResponse(
            definition.Code,
            definition.Name,
            [.. definition.Fields.Select(static f => new PrintDataSourceFieldResponse(
                f.Key,
                f.Label,
                f.Kind,
                f.Columns?.Select(static c => new PrintDataSourceColumnResponse(c.Field, c.Title, c.Width, c.InputType, c.Placeholder)).ToList(),
                f.InputType,
                f.Placeholder))],
            definition.SampleDataJson);
    }
}

/// <summary>
/// 打印数据源的一个字段
/// </summary>
/// <param name="Key">数据字段路径</param>
/// <param name="Label">设计器素材的标题</param>
/// <param name="Kind">元素种类：text / image / barcode / qrcode / table</param>
/// <param name="Columns">明细表的列（只有 table 有）</param>
/// <param name="InputType">样例表单的控件</param>
/// <param name="Placeholder">样例表单的占位文案</param>
public sealed record PrintDataSourceFieldResponse(string Key, string Label, string Kind, IReadOnlyList<PrintDataSourceColumnResponse>? Columns, string? InputType, string? Placeholder);

/// <summary>
/// 打印数据源明细表的一列
/// </summary>
/// <param name="Field">数据字段路径</param>
/// <param name="Title">列标题</param>
/// <param name="Width">列宽（pt）</param>
/// <param name="InputType">样例表格单元格的控件</param>
/// <param name="Placeholder">样例表格单元格的占位文案</param>
public sealed record PrintDataSourceColumnResponse(string Field, string Title, int? Width, string? InputType, string? Placeholder);
