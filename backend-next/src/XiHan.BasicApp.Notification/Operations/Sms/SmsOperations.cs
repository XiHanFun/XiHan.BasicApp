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

namespace XiHan.BasicApp.Notification.Operations.Sms;

/// <summary>
/// 分页列出当前一侧的短信发送记录：<c>POST /api/sms:list</c>。
/// </summary>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsRead)]
public sealed class ListSms(MessagingQueries queries) : IOperation<PageRequest, PageResult<SmsRecordResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<SmsRecordResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.SmsAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一条短信发送记录：<c>GET /api/sms/{id}</c>。
/// </summary>
/// <param name="sms">记录仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsRead)]
public sealed class GetSms(IRepository<SysSms, Guid> sms) : IOperation<MessagingIdRequest, SmsRecordResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms.get";

    /// <inheritdoc />
    public async ValueTask<Result<SmsRecordResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await sms.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var record) && string.Equals(record.OwnerTenantId, SideRows.SideOf(context), StringComparison.Ordinal)
            ? SmsRecordResponse.From(record)
            : Error.NotFound(NotificationErrors.SmsRecordNotFound, NotificationMessages.SmsRecordNotFound, MessageArguments.Of(("id", request.Id)));
    }
}

/// <summary>
/// 重发短信：<c>POST /api/sms:resend</c>。
/// </summary>
/// <param name="sms">记录仓储。</param>
/// <param name="service">记录服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsResend)]
public sealed class ResendSms(IRepository<SysSms, Guid> sms, MessageRecordService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms.resend";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.ResendAsync(sms, SideRows.SideOf(context), request.Ids, NotificationErrors.SmsRecordNotFound, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 取消排队中的短信：<c>POST /api/sms:cancel</c>。
/// </summary>
/// <param name="sms">记录仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsResend)]
public sealed class CancelSms(IRepository<SysSms, Guid> sms) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms.cancel";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await MessageRecordService.CancelAsync(sms, SideRows.SideOf(context), request.Ids, NotificationErrors.SmsRecordNotFound, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量删除短信发送记录。
/// </summary>
/// <remarks>
/// <c>DELETE /api/sms?ids=</c>（发送中的逐项拒绝）。
/// </remarks>
/// <param name="sms">记录仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsDelete)]
public sealed class DeleteSms(IRepository<SysSms, Guid> sms) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await MessageRecordService.DeleteAsync(sms, SideRows.SideOf(context), request.Ids, NotificationErrors.SmsRecordNotFound, context.CancellationToken).ConfigureAwait(false);
    }
}
