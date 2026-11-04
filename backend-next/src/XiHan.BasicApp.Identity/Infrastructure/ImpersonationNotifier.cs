// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 告诉被模仿者有人以他的身份登录（作用域）
/// </summary>
/// <remarks>
/// 经 Contracts 的站内投递发一条安全通知（4.x 同：不看收件人的通知偏好，投递失败只记日志、不阻断模仿）。文案按被模仿者的界面语言，没存语言的按发起这次调用的区域
/// 通知落在被模仿者要去的那一侧：平台主体模仿租户里的人时另起一次租户调用，独立提交（在租户调用里写平台调用的事务拿不到），所以要在发起调用的第一次写入之前调用，SQLite 上才不会等自己的写锁
/// </remarks>
/// <param name="deliveries">站内投递（Notification 实现）</param>
/// <param name="locales">用户的界面语言</param>
/// <param name="localizer">消息本地化</param>
/// <param name="runner">按租户发起调用的运行器</param>
/// <param name="logger">日志</param>
public sealed partial class ImpersonationNotifier(IEnumerable<IInboxDelivery> deliveries, IUserLocales locales, IMessageLocalizer localizer, ITenantScopeRunner runner, ILogger<ImpersonationNotifier> logger)
{
    /// <summary>
    /// 站内信的业务类型
    /// </summary>
    public const string BusinessType = "auth.impersonation";

    /// <summary>
    /// 按参数给被模仿者投一条安全通知
    /// </summary>
    /// <param name="target">被模仿者</param>
    /// <param name="actor">发起人</param>
    /// <param name="targetTenant">模仿会话所在的租户；平台为 <see langword="null"/></param>
    /// <param name="settings">模仿登录的参数</param>
    /// <param name="context">调用上下文</param>
    public async Task NotifyAsync(SysUser target, SysUser actor, string? targetTenant, ImpersonationSettings settings, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(context);
        if (!settings.NotifyTarget || !deliveries.Any())
        {
            return;
        }

        var preferred = await locales.PreferredAsync([target.Id], context.CancellationToken).ConfigureAwait(false);
        var locale = preferred.TryGetValue(target.Id, out var stored) ? localizer.Options.Negotiate([stored]) : localizer.Options.Resolve(context);
        var message = new InboxMessage(
            [target.Id],
            Render(locale, IdentityMessages.ImpersonationNoticeTitle),
            Render(locale, IdentityMessages.ImpersonationNoticeContent, ("actor", actor.UserName), ("minutes", settings.SessionMinutes)),
            Link: null,
            BusinessType,
            actor.Id.ToString("D"),
            InboxMessageKind.Security);
        var delivered = string.Equals(context.GetSecurity().Tenant?.Id, targetTenant, StringComparison.Ordinal)
            ? await DeliverAsync(deliveries, context, message).ConfigureAwait(false)
            : await runner.RunAsync(targetTenant!, ctx => new ValueTask<Result>(DeliverAsync(ctx.Services.GetServices<IInboxDelivery>(), ctx, message)), context.CancellationToken).ConfigureAwait(false);
        if (delivered.IsFailure)
        {
            LogNotDelivered(logger, delivered.Error!.Code, target.Id);
        }
    }

    private static async Task<Result> DeliverAsync(IEnumerable<IInboxDelivery> targets, InvocationContext context, InboxMessage message)
    {
        foreach (var delivery in targets)
        {
            var delivered = await delivery.DeliverAsync(context, message).ConfigureAwait(false);
            if (delivered.IsFailure)
            {
                return delivered.Error!;
            }
        }

        return Result.Success();
    }

    // 键都在 10 种区域里，取不到是缺陷，直接抛出
    private string Render(string locale, string key, params (string Name, object? Value)[] arguments)
    {
        var values = arguments.ToDictionary(static a => a.Name, static a => a.Value, StringComparer.Ordinal);
        return localizer.Resolve(key, locale, values).TryGetValue(out var text)
            ? text.Text
            : throw new InvalidOperationException($"Identity text '{key}' is missing for locale '{locale}'.");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The impersonation notice was not delivered ({Code}) to {UserId}; the impersonation goes on.")]
    private static partial void LogNotDelivered(ILogger logger, string code, Guid userId);
}
