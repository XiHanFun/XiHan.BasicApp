// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Messaging;
using XiHan.BasicApp.Notification.Queries;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Operations.Emails;

/// <summary>
/// 分页列出当前一侧的邮件发送记录：<c>POST /api/emails:list</c>。
/// </summary>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailsRead)]
public sealed class ListEmails(MessagingQueries queries) : IOperation<PageRequest, PageResult<EmailRecordResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "emails.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<EmailRecordResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.EmailsAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一条邮件发送记录：<c>GET /api/emails/{id}</c>。
/// </summary>
/// <param name="emails">记录仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailsRead)]
public sealed class GetEmail(IRepository<SysEmail, Guid> emails) : IOperation<MessagingIdRequest, EmailRecordResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "emails.get";

    /// <inheritdoc />
    public async ValueTask<Result<EmailRecordResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await emails.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var record) && string.Equals(record.OwnerTenantId, SideRows.SideOf(context), StringComparison.Ordinal)
            ? EmailRecordResponse.From(record)
            : Error.NotFound(NotificationErrors.EmailRecordNotFound, NotificationMessages.EmailRecordNotFound, MessageArguments.Of(("id", request.Id)));
    }
}

/// <summary>
/// 重发邮件。
/// </summary>
/// <remarks>
/// <c>POST /api/emails:resend</c>（改回排队中、清零尝试次数；排队中与发送中的逐项拒绝）。
/// </remarks>
/// <param name="emails">记录仓储。</param>
/// <param name="service">记录服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailsResend)]
public sealed class ResendEmails(IRepository<SysEmail, Guid> emails, MessageRecordService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "emails.resend";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.ResendAsync(emails, SideRows.SideOf(context), request.Ids, NotificationErrors.EmailRecordNotFound, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 取消排队中的邮件：<c>POST /api/emails:cancel</c>。
/// </summary>
/// <param name="emails">记录仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailsResend)]
public sealed class CancelEmails(IRepository<SysEmail, Guid> emails) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "emails.cancel";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await MessageRecordService.CancelAsync(emails, SideRows.SideOf(context), request.Ids, NotificationErrors.EmailRecordNotFound, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量删除邮件发送记录。
/// </summary>
/// <remarks>
/// <c>DELETE /api/emails?ids=</c>（发送中的逐项拒绝）。
/// </remarks>
/// <param name="emails">记录仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailsDelete)]
public sealed class DeleteEmails(IRepository<SysEmail, Guid> emails) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "emails.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await MessageRecordService.DeleteAsync(emails, SideRows.SideOf(context), request.Ids, NotificationErrors.EmailRecordNotFound, context.CancellationToken).ConfigureAwait(false);
    }
}
