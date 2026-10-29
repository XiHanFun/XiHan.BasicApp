// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.LoginLogs;

/// <summary>
/// 登录日志列表。
/// </summary>
/// <remarks>
/// <c>POST /api/login-logs:list</c>。只看当前一侧（平台主体看平台的，租户主体看本租户的）。
/// </remarks>
/// <param name="queries">登录日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.LoginLogsRead)]
public sealed class ListLoginLogs(LoginLogQueries queries) : IOperation<PageRequest, PageResult<LoginLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "login-logs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<LoginLogResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
