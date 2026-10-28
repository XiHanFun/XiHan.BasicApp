// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.LoginLogs;

/// <summary>
/// 登录日志详情：<c>GET /api/login-logs/{id}</c>。不在当前一侧即找不到。
/// </summary>
/// <param name="queries">登录日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.LoginLogsRead)]
public sealed class GetLoginLog(LoginLogQueries queries) : IOperation<GetLoginLogRequest, LoginLogResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "login-logs.get";

    /// <inheritdoc />
    public ValueTask<Result<LoginLogResponse>> HandleAsync(GetLoginLogRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
