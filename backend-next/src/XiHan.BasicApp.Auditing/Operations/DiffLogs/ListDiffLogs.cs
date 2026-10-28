// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.DiffLogs;

/// <summary>
/// 数据变更日志列表：<c>POST /api/diff-logs:list</c>。只看当前一侧（平台主体看平台的，租户主体看本租户的）。
/// </summary>
/// <param name="queries">数据变更日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.DiffLogsRead)]
public sealed class ListDiffLogs(DiffLogQueries queries) : IOperation<PageRequest, PageResult<DiffLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "diff-logs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<DiffLogResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
