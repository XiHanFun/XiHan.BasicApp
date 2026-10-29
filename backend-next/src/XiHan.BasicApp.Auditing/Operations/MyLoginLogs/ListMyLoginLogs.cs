// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.MyLoginLogs;

/// <summary>
/// <c>my-login-logs.list</c> 的请求。
/// </summary>
/// <param name="Page">页码，从 1 起。</param>
/// <param name="PageSize">页大小（1–50，缺省 10；4.x 同）。</param>
public sealed record MyLoginLogsRequest([Range(1, 10000)] int Page = 1, [Range(1, 50)] int PageSize = 10);

/// <summary>
/// 本人的一条登录日志。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="Event">事件。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="Device">设备类型。</param>
/// <param name="Impersonated">是否别人模仿本人时记下的。</param>
public sealed record MyLoginLogResponse(Guid Id, DateTimeOffset OccurredAt, LoginEvent Event, bool Succeeded, string? ErrorCode, string? ErrorMessage, string? ClientIp, string? Browser, string? Os, string? Device, bool Impersonated);

/// <summary>
/// 本人的登录日志。
/// </summary>
/// <remarks>
/// <c>GET /api/my-login-logs</c>（4.x <c>Profile/LoginLogs</c>）。只按本人（不分平台与租户一侧、扫全部月表），按发生时间倒序；已登录即可。
/// 模仿会话里看到的是被模仿的人的日志（4.x 同）。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListMyLoginLogs(SqlSugarUnitOfWork unitOfWork) : IOperation<MyLoginLogsRequest, PageResult<MyLoginLogResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-login-logs.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<MyLoginLogResponse>>> HandleAsync(MyLoginLogsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.GetSecurity().Principal;
        if (!Guid.TryParse(principal.Id, out var userId))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
        }

        // 条件在分表之前施加（下推到每张月表），排序与分页在之后
        var query = unitOfWork.ConnectionFor(typeof(SysLoginLog)).Queryable<SysLoginLog>()
            .Where(e => e.UserId == userId)
            .SplitTable()
            .OrderBy(e => e.OccurredAt, OrderByType.Desc)
            .OrderBy(e => e.Id, OrderByType.Desc);
        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page, request.PageSize, total, context.CancellationToken).ConfigureAwait(false);
        var items = rows.Select(static log => new MyLoginLogResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.Event,
            log.Succeeded,
            log.ErrorCode,
            log.ErrorMessage,
            log.ClientIp,
            log.Browser,
            log.Os,
            log.Device,
            log.ImpersonatorUserId.HasValue)).ToList();
        return new PageResult<MyLoginLogResponse>(items, total.Value, new PageInfo { Index = request.Page, Size = request.PageSize });
    }
}
