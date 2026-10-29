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

namespace XiHan.BasicApp.Notification.Operations.EmailConfigs;

/// <summary>
/// 分页列出当前一侧的邮件配置。
/// </summary>
/// <remarks>
/// <c>POST /api/email-configs:list</c>（密码不回显）。
/// </remarks>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailConfigsRead)]
public sealed class ListEmailConfigs(MessagingQueries queries) : IOperation<PageRequest, PageResult<EmailConfigResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "email-configs.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<EmailConfigResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.EmailConfigsAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一个邮件配置：<c>GET /api/email-configs/{id}</c>。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailConfigsRead)]
public sealed class GetEmailConfig(ChannelConfigService service) : IOperation<MessagingIdRequest, EmailConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "email-configs.get";

    /// <inheritdoc />
    public async ValueTask<Result<EmailConfigResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindEmailAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) ? EmailConfigResponse.From(config) : found.Error!;
    }
}

/// <summary>
/// 新建邮件配置。
/// </summary>
/// <remarks>
/// <c>POST /api/email-configs</c>（本侧还没有缺省配置即成为缺省）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailConfigsCreate)]
public sealed class CreateEmailConfig(ChannelConfigService service) : IOperation<CreateEmailConfigRequest, EmailConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "email-configs.create";

    /// <inheritdoc />
    public async ValueTask<Result<EmailConfigResponse>> HandleAsync(CreateEmailConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateEmailAsync(SideRows.SideOf(context), request.Code, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var config) ? EmailConfigResponse.From(config) : created.Error!;
    }
}

/// <summary>
/// 修改邮件配置。
/// </summary>
/// <remarks>
/// <c>PUT /api/email-configs/{id}</c>（密码不填即保留；缺省配置不能停用）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailConfigsUpdate)]
public sealed class UpdateEmailConfig(ChannelConfigService service) : IOperation<UpdateEmailConfigRequest, EmailConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "email-configs.update";

    /// <inheritdoc />
    public async ValueTask<Result<EmailConfigResponse>> HandleAsync(UpdateEmailConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindEmailAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var config))
        {
            return found.Error!;
        }

        var updated = await service.UpdateEmailAsync(config, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : EmailConfigResponse.From(config);
    }
}

/// <summary>
/// 设为缺省邮件配置。
/// </summary>
/// <remarks>
/// <c>POST /api/email-configs/{id}:set-default</c>（须启用）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailConfigsUpdate)]
public sealed class SetDefaultEmailConfig(ChannelConfigService service) : IOperation<MessagingIdRequest, EmailConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "email-configs.set-default";

    /// <inheritdoc />
    public async ValueTask<Result<EmailConfigResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindEmailAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var config))
        {
            return found.Error!;
        }

        var set = await service.SetDefaultEmailAsync(config, context.CancellationToken).ConfigureAwait(false);
        return set.IsFailure ? set.Error! : EmailConfigResponse.From(config);
    }
}

/// <summary>
/// 批量删除邮件配置。
/// </summary>
/// <remarks>
/// <c>DELETE /api/email-configs?ids=</c>（缺省配置逐项拒绝）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.EmailConfigsDelete)]
public sealed class DeleteEmailConfigs(ChannelConfigService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "email-configs.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteEmailsAsync(SideRows.SideOf(context), request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}
