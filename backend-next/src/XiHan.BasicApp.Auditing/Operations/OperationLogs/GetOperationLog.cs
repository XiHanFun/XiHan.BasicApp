// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.OperationLogs;

/// <summary>
/// 操作日志详情。
/// </summary>
/// <remarks>
/// <c>GET /api/operation-logs/{id}</c>。不在当前一侧即找不到。
/// </remarks>
/// <param name="queries">操作日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.OperationLogsRead)]
public sealed class GetOperationLog(OperationLogQueries queries) : IOperation<GetOperationLogRequest, OperationLogDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "operation-logs.get";

    /// <inheritdoc />
    public ValueTask<Result<OperationLogDetailResponse>> HandleAsync(GetOperationLogRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
