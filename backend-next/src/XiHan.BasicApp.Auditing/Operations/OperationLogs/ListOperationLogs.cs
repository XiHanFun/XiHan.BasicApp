// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.OperationLogs;

/// <summary>
/// 操作日志列表：<c>POST /api/operation-logs:list</c>。只看当前一侧（平台主体看没有租户的记录，租户主体看本租户的）。
/// </summary>
/// <param name="queries">操作日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.OperationLogsRead)]
public sealed class ListOperationLogs(OperationLogQueries queries) : IOperation<PageRequest, PageResult<OperationLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "operation-logs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<OperationLogResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
