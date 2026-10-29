// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.AccessLogs;

/// <summary>
/// 访问日志列表。
/// </summary>
/// <remarks>
/// <c>POST /api/access-logs:list</c>。只看当前一侧（平台主体看平台的，租户主体看本租户的）。
/// </remarks>
/// <param name="queries">访问日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.AccessLogsRead)]
public sealed class ListAccessLogs(AccessLogQueries queries) : IOperation<PageRequest, PageResult<AccessLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "access-logs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<AccessLogResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
