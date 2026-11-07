// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Settings.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 字典项列表
/// </summary>
/// <remarks>
/// <c>POST /api/dict-items:list</c>。读共享：平台字典与本租户字典的项，按 <c>dictId</c> 筛选即某本字典的项；关键字匹配编码、名称与附加值
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）；整本字典的层级走 <c>dict-items.tree</c>
/// </remarks>
/// <param name="queries">字典项查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = SettingsPermissions.DictItemsRead)]
public sealed class ListDictItems(DictItemQueries queries) : IOperation<PageRequest, PageResult<DictItemResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "dict-items.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<DictItemResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
