// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.DiffLogs;

/// <summary>
/// 数据变更日志详情：<c>GET /api/diff-logs/{id}</c>。不在当前一侧即找不到。
/// </summary>
/// <param name="queries">数据变更日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.DiffLogsRead)]
public sealed class GetDiffLog(DiffLogQueries queries) : IOperation<GetDiffLogRequest, DiffLogDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "diff-logs.get";

    /// <inheritdoc />
    public ValueTask<Result<DiffLogDetailResponse>> HandleAsync(GetDiffLogRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
