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

namespace XiHan.BasicApp.Notification.Operations.SmsConfigs;

/// <summary>
/// 分页列出当前一侧的短信配置。
/// </summary>
/// <remarks>
/// <c>POST /api/sms-configs:list</c>（访问密钥不回显）。
/// </remarks>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsConfigsRead)]
public sealed class ListSmsConfigs(MessagingQueries queries) : IOperation<PageRequest, PageResult<SmsConfigResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms-configs.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<SmsConfigResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.SmsConfigsAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一个短信配置：<c>GET /api/sms-configs/{id}</c>。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsConfigsRead)]
public sealed class GetSmsConfig(ChannelConfigService service) : IOperation<MessagingIdRequest, SmsConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms-configs.get";

    /// <inheritdoc />
    public async ValueTask<Result<SmsConfigResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindSmsAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) ? SmsConfigResponse.From(config) : found.Error!;
    }
}

/// <summary>
/// 新建短信配置。
/// </summary>
/// <remarks>
/// <c>POST /api/sms-configs</c>（模板映射须是 JSON 对象，<c>NOTIFICATION_VALIDATION_009</c>）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsConfigsCreate)]
public sealed class CreateSmsConfig(ChannelConfigService service) : IOperation<CreateSmsConfigRequest, SmsConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms-configs.create";

    /// <inheritdoc />
    public async ValueTask<Result<SmsConfigResponse>> HandleAsync(CreateSmsConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateSmsAsync(SideRows.SideOf(context), request.Code, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var config) ? SmsConfigResponse.From(config) : created.Error!;
    }
}

/// <summary>
/// 修改短信配置。
/// </summary>
/// <remarks>
/// <c>PUT /api/sms-configs/{id}</c>（访问密钥不填即保留；缺省配置不能停用）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsConfigsUpdate)]
public sealed class UpdateSmsConfig(ChannelConfigService service) : IOperation<UpdateSmsConfigRequest, SmsConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms-configs.update";

    /// <inheritdoc />
    public async ValueTask<Result<SmsConfigResponse>> HandleAsync(UpdateSmsConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindSmsAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var config))
        {
            return found.Error!;
        }

        var updated = await service.UpdateSmsAsync(config, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : SmsConfigResponse.From(config);
    }
}

/// <summary>
/// 设为缺省短信配置。
/// </summary>
/// <remarks>
/// <c>POST /api/sms-configs/{id}:set-default</c>（须启用）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsConfigsUpdate)]
public sealed class SetDefaultSmsConfig(ChannelConfigService service) : IOperation<MessagingIdRequest, SmsConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms-configs.set-default";

    /// <inheritdoc />
    public async ValueTask<Result<SmsConfigResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindSmsAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var config))
        {
            return found.Error!;
        }

        var set = await service.SetDefaultSmsAsync(config, context.CancellationToken).ConfigureAwait(false);
        return set.IsFailure ? set.Error! : SmsConfigResponse.From(config);
    }
}

/// <summary>
/// 批量删除短信配置。
/// </summary>
/// <remarks>
/// <c>DELETE /api/sms-configs?ids=</c>（缺省配置逐项拒绝）。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.SmsConfigsDelete)]
public sealed class DeleteSmsConfigs(ChannelConfigService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sms-configs.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteSmsAsync(SideRows.SideOf(context), request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}
