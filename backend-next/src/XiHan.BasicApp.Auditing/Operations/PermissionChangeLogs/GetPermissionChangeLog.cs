// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.PermissionChangeLogs;

/// <summary>
/// 权限变更日志详情。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-change-logs/{id}</c>。不在当前一侧即找不到。
/// </remarks>
/// <param name="queries">权限变更日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.PermissionChangeLogsRead)]
public sealed class GetPermissionChangeLog(PermissionChangeLogQueries queries) : IOperation<GetPermissionChangeLogRequest, PermissionChangeLogResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-change-logs.get";

    /// <inheritdoc />
    public ValueTask<Result<PermissionChangeLogResponse>> HandleAsync(GetPermissionChangeLogRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
