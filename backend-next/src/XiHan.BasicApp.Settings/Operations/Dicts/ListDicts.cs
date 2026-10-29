// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Settings.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 字典列表。
/// </summary>
/// <remarks>
/// <c>POST /api/dicts:list</c>。读共享：平台字典与本租户字典。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </remarks>
/// <param name="queries">字典查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = SettingsPermissions.DictsRead)]
public sealed class ListDicts(DictQueries queries) : IOperation<PageRequest, PageResult<DictResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dicts.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<DictResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
