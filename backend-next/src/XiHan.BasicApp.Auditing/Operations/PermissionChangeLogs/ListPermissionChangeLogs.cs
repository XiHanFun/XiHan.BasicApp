// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.PermissionChangeLogs;

/// <summary>
/// 权限变更日志列表。
/// </summary>
/// <remarks>
/// <c>POST /api/permission-change-logs:list</c>。只看当前一侧（平台主体看平台的，租户主体看本租户的）。
/// </remarks>
/// <param name="queries">权限变更日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.PermissionChangeLogsRead)]
public sealed class ListPermissionChangeLogs(PermissionChangeLogQueries queries) : IOperation<PageRequest, PageResult<PermissionChangeLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-change-logs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<PermissionChangeLogResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
