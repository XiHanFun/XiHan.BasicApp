// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Messaging;
using XiHan.BasicApp.Notification.Queries;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Operations.MessageTemplates;

/// <summary>
/// 分页列出当前一侧的消息模板：<c>POST /api/message-templates:list</c>。
/// </summary>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TemplatesRead)]
public sealed class ListMessageTemplates(MessagingQueries queries) : IOperation<PageRequest, PageResult<MessageTemplateResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "message-templates.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<MessageTemplateResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.TemplatesAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一个消息模板：<c>GET /api/message-templates/{id}</c>。
/// </summary>
/// <param name="service">模板服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TemplatesRead)]
public sealed class GetMessageTemplate(MessageTemplateService service) : IOperation<MessagingIdRequest, MessageTemplateResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "message-templates.get";

    /// <inheritdoc />
    public async ValueTask<Result<MessageTemplateResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var template) ? MessageTemplateResponse.From(template) : found.Error!;
    }
}

/// <summary>
/// 新建消息模板：<c>POST /api/message-templates</c>。编码在本侧、同一渠道内唯一（<c>NOTIFICATION_CONFLICT_003</c>），主题与正文须能解析（<c>NOTIFICATION_VALIDATION_007</c>）。
/// </summary>
/// <param name="service">模板服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TemplatesCreate)]
public sealed class CreateMessageTemplate(MessageTemplateService service) : IOperation<CreateMessageTemplateRequest, MessageTemplateResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "message-templates.create";

    /// <inheritdoc />
    public async ValueTask<Result<MessageTemplateResponse>> HandleAsync(CreateMessageTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(SideRows.SideOf(context), request.Code, request.Channel, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var template) ? MessageTemplateResponse.From(template) : created.Error!;
    }
}

/// <summary>
/// 修改消息模板：<c>PUT /api/message-templates/{id}</c>（编码与渠道不改）。
/// </summary>
/// <param name="service">模板服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TemplatesUpdate)]
public sealed class UpdateMessageTemplate(MessageTemplateService service) : IOperation<UpdateMessageTemplateRequest, MessageTemplateResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "message-templates.update";

    /// <inheritdoc />
    public async ValueTask<Result<MessageTemplateResponse>> HandleAsync(UpdateMessageTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var template))
        {
            return found.Error!;
        }

        var updated = await service.UpdateAsync(template, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : MessageTemplateResponse.From(template);
    }
}

/// <summary>
/// 批量删除消息模板：<c>DELETE /api/message-templates?ids=</c>（平台的全局模板只有平台能删；租户删了自己的同编码模板即回落到平台的）。
/// </summary>
/// <param name="service">模板服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TemplatesDelete)]
public sealed class DeleteMessageTemplates(MessageTemplateService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "message-templates.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(SideRows.SideOf(context), request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}
