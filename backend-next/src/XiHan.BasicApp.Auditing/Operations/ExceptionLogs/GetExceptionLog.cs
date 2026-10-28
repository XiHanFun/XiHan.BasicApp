// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.ExceptionLogs;

/// <summary>
/// 异常日志详情：<c>GET /api/exception-logs/{id}</c>。不在当前一侧即找不到。
/// </summary>
/// <param name="queries">异常日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.ExceptionLogsRead)]
public sealed class GetExceptionLog(ExceptionLogQueries queries) : IOperation<GetExceptionLogRequest, ExceptionLogDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "exception-logs.get";

    /// <inheritdoc />
    public ValueTask<Result<ExceptionLogDetailResponse>> HandleAsync(GetExceptionLogRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
