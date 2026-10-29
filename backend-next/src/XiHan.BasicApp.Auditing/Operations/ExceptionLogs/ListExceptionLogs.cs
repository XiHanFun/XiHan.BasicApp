// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.ExceptionLogs;

/// <summary>
/// 异常日志列表。
/// </summary>
/// <remarks>
/// <c>POST /api/exception-logs:list</c>。只看当前一侧（平台主体看平台的，租户主体看本租户的）。
/// </remarks>
/// <param name="queries">异常日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.ExceptionLogsRead)]
public sealed class ListExceptionLogs(ExceptionLogQueries queries) : IOperation<PageRequest, PageResult<ExceptionLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "exception-logs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ExceptionLogResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
