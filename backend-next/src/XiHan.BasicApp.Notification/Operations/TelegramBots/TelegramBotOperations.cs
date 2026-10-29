// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Messaging;
using XiHan.BasicApp.Notification.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Operations.TelegramBots;

/// <summary>
/// 分页列出 Telegram 机器人。
/// </summary>
/// <remarks>
/// <c>POST /api/telegram-bots:list</c>（平台托管；令牌不回显）。
/// </remarks>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TelegramBotsRead)]
public sealed class ListTelegramBots(MessagingQueries queries) : IOperation<PageRequest, PageResult<TelegramBotResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "telegram-bots.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<TelegramBotResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.TelegramBotsAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一个 Telegram 机器人。
/// </summary>
/// <remarks>
/// <c>GET /api/telegram-bots/{id}</c>。
/// </remarks>
/// <param name="service">机器人服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TelegramBotsRead)]
public sealed class GetTelegramBot(TelegramBotService service) : IOperation<MessagingIdRequest, TelegramBotResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "telegram-bots.get";

    /// <inheritdoc />
    public async ValueTask<Result<TelegramBotResponse>> HandleAsync(MessagingIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var bot) ? TelegramBotResponse.From(bot) : found.Error!;
    }
}

/// <summary>
/// 新建 Telegram 机器人。
/// </summary>
/// <remarks>
/// <c>POST /api/telegram-bots</c>（名称唯一 <c>NOTIFICATION_CONFLICT_008</c>、令牌格式 <c>NOTIFICATION_VALIDATION_012</c>、ID 列表须是整数 <c>013</c>、命令名 <c>014</c>）。
/// </remarks>
/// <param name="service">机器人服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TelegramBotsCreate)]
public sealed class CreateTelegramBot(TelegramBotService service) : IOperation<CreateTelegramBotRequest, TelegramBotResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "telegram-bots.create";

    /// <inheritdoc />
    public async ValueTask<Result<TelegramBotResponse>> HandleAsync(CreateTelegramBotRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var bot) ? TelegramBotResponse.From(bot) : created.Error!;
    }
}

/// <summary>
/// 修改 Telegram 机器人。
/// </summary>
/// <remarks>
/// <c>PUT /api/telegram-bots/{id}</c>（令牌不填即保留）。
/// </remarks>
/// <param name="service">机器人服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TelegramBotsUpdate)]
public sealed class UpdateTelegramBot(TelegramBotService service) : IOperation<UpdateTelegramBotRequest, TelegramBotResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "telegram-bots.update";

    /// <inheritdoc />
    public async ValueTask<Result<TelegramBotResponse>> HandleAsync(UpdateTelegramBotRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var bot))
        {
            return found.Error!;
        }

        var updated = await service.UpdateAsync(bot, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : TelegramBotResponse.From(bot);
    }
}

/// <summary>
/// 批量删除 Telegram 机器人。
/// </summary>
/// <remarks>
/// <c>DELETE /api/telegram-bots?ids=</c>。
/// </remarks>
/// <param name="service">机器人服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.TelegramBotsDelete)]
public sealed class DeleteTelegramBots(TelegramBotService service) : IOperation<MessagingIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "telegram-bots.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MessagingIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}
