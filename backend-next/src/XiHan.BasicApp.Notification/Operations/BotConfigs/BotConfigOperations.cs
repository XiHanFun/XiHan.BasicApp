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

namespace XiHan.BasicApp.Notification.Operations.BotConfigs;

/// <summary>
/// 分页列出当前一侧的机器人配置：<c>POST /api/bot-configs:list</c>（签名密钥不回显）。
/// </summary>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.BotConfigsRead)]
public sealed class ListBotConfigs(MessagingQueries queries) : IOperation<PageRequest, PageResult<BotConfigResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "bot-configs.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<BotConfigResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.BotConfigsAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一个机器人配置：<c>GET /api/bot-configs/{id}</c>。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.BotConfigsRead)]
public sealed class GetBotConfig(BotConfigService service) : IOperation<MessagingIdRequest, BotConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "bot-configs.get";

    /// <inheritdoc />
    public async ValueTask<Result<BotConfigResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) ? BotConfigResponse.From(config) : found.Error!;
    }
}

/// <summary>
/// 新建机器人配置：<c>POST /api/bot-configs</c>（Webhook 只认服务商的官方 https 地址，<c>NOTIFICATION_VALIDATION_011</c>；这个服务商的第一个即缺省）。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.BotConfigsCreate)]
public sealed class CreateBotConfig(BotConfigService service) : IOperation<CreateBotConfigRequest, BotConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "bot-configs.create";

    /// <inheritdoc />
    public async ValueTask<Result<BotConfigResponse>> HandleAsync(CreateBotConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(SideRows.SideOf(context), request.Code, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var config) ? BotConfigResponse.From(config) : created.Error!;
    }
}

/// <summary>
/// 修改机器人配置：<c>PUT /api/bot-configs/{id}</c>（签名密钥不填即保留；缺省的不能停用也不能换服务商）。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.BotConfigsUpdate)]
public sealed class UpdateBotConfig(BotConfigService service) : IOperation<UpdateBotConfigRequest, BotConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "bot-configs.update";

    /// <inheritdoc />
    public async ValueTask<Result<BotConfigResponse>> HandleAsync(UpdateBotConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var config))
        {
            return found.Error!;
        }

        var updated = await service.UpdateAsync(config, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : BotConfigResponse.From(config);
    }
}

/// <summary>
/// 设为这个服务商的缺省机器人：<c>POST /api/bot-configs/{id}:set-default</c>（须启用）。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.BotConfigsUpdate)]
public sealed class SetDefaultBotConfig(BotConfigService service) : IOperation<MessagingIdRequest, BotConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "bot-configs.set-default";

    /// <inheritdoc />
    public async ValueTask<Result<BotConfigResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var config))
        {
            return found.Error!;
        }

        var set = await service.SetDefaultAsync(config, context.CancellationToken).ConfigureAwait(false);
        return set.IsFailure ? set.Error! : BotConfigResponse.From(config);
    }
}

/// <summary>
/// 批量删除机器人配置：<c>DELETE /api/bot-configs?ids=</c>（缺省的逐项拒绝）。
/// </summary>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.BotConfigsDelete)]
public sealed class DeleteBotConfigs(BotConfigService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "bot-configs.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(SideRows.SideOf(context), request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}
