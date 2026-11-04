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
/// 发给账号本人的安全通知（作用域）
/// </summary>
/// <remarks>
/// 经 Contracts 的站内投递发一条安全通知（4.x 同：不看收件人的通知偏好，投递失败只记日志、不挡业务）。文案按收件人的界面语言，没存语言的按发起这次调用的区域
/// 通知落在收件人要去的那一侧：调用不在那一侧时（平台主体模仿租户里的人、匿名的登录调用）另起一次租户调用，独立提交（调用自己的事务写不到那一侧），所以要在调用第一次写入之前调用，SQLite 上才不会等自己的写锁
/// </remarks>
/// <param name="deliveries">站内投递（Notification 实现）</param>
/// <param name="locales">用户的界面语言</param>
/// <param name="localizer">消息本地化</param>
/// <param name="runner">按租户发起调用的运行器</param>
/// <param name="logger">日志</param>
public sealed partial class SecurityNotices(IEnumerable<IInboxDelivery> deliveries, IUserLocales locales, IMessageLocalizer localizer, ITenantScopeRunner runner, ILogger<SecurityNotices> logger)
{
    /// <summary>
    /// 模仿登录通知的业务类型
    /// </summary>
    public const string ImpersonationBusinessType = "auth.impersonation";

    /// <summary>
    /// 强制改密提醒的业务类型
    /// </summary>
    public const string PasswordChangeBusinessType = "auth.password-change-required";

    /// <summary>
    /// 个人中心的前端路由
    /// </summary>
    public const string ProfileLink = "/profile";

    /// <summary>
    /// 告诉被模仿者有人以他的身份登录
    /// </summary>
    /// <remarks>
    /// 参数 <see cref="ImpersonationSettings.NotifyTarget"/> 关着即不发
    /// </remarks>
    /// <param name="target">被模仿者</param>
    /// <param name="actor">发起人</param>
    /// <param name="targetTenant">模仿会话所在的租户；平台为 <see langword="null"/></param>
    /// <param name="settings">模仿登录的参数</param>
    /// <param name="context">调用上下文</param>
    public Task ImpersonationAsync(SysUser target, SysUser actor, string? targetTenant, ImpersonationSettings settings, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(settings);
        return !settings.NotifyTarget
            ? Task.CompletedTask
            : DeliverAsync(target.Id, targetTenant, IdentityMessages.ImpersonationNoticeTitle, IdentityMessages.ImpersonationNoticeContent, [("actor", actor.UserName), ("minutes", settings.SessionMinutes)], link: null, ImpersonationBusinessType, actor.Id, context);
    }

    /// <summary>
    /// 提醒账号本人要先改密
    /// </summary>
    /// <param name="user">账号</param>
    /// <param name="tenant">登录的上下文；平台为 <see langword="null"/></param>
    /// <param name="context">调用上下文</param>
    public Task PasswordChangeRequiredAsync(SysUser user, string? tenant, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        return DeliverAsync(user.Id, tenant, IdentityMessages.PasswordChangeNoticeTitle, IdentityMessages.PasswordChangeNoticeContent, [], ProfileLink, PasswordChangeBusinessType, user.Id, context);
    }

    private async Task DeliverAsync(Guid userId, string? tenant, string titleKey, string contentKey, (string Name, object? Value)[] arguments, string? link, string businessType, Guid businessId, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!deliveries.Any())
        {
            return;
        }

        var preferred = await locales.PreferredAsync([userId], context.CancellationToken).ConfigureAwait(false);
        var locale = preferred.TryGetValue(userId, out var stored) ? localizer.Options.Negotiate([stored]) : localizer.Options.Resolve(context);
        var message = new InboxMessage([userId], Render(locale, titleKey, []), Render(locale, contentKey, arguments), link, businessType, businessId.ToString("D"), InboxMessageKind.Security);
        var delivered = string.Equals(context.GetSecurity().Tenant?.Id, tenant, StringComparison.Ordinal)
            ? await DeliverToAsync(deliveries, context, message).ConfigureAwait(false)
            : await runner.RunAsync(tenant!, ctx => new ValueTask<Result>(DeliverToAsync(ctx.Services.GetServices<IInboxDelivery>(), ctx, message)), context.CancellationToken).ConfigureAwait(false);
        if (delivered.IsFailure)
        {
            LogNotDelivered(logger, businessType, delivered.Error!.Code, userId);
        }
    }

    private static async Task<Result> DeliverToAsync(IEnumerable<IInboxDelivery> targets, InvocationContext context, InboxMessage message)
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
    private string Render(string locale, string key, (string Name, object? Value)[] arguments)
    {
        var values = arguments.ToDictionary(static a => a.Name, static a => a.Value, StringComparer.Ordinal);
        return localizer.Resolve(key, locale, values).TryGetValue(out var text)
            ? text.Text
            : throw new InvalidOperationException($"Identity text '{key}' is missing for locale '{locale}'.");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The {BusinessType} notice was not delivered ({Code}) to {UserId}; the caller goes on.")]
    private static partial void LogNotDelivered(ILogger logger, string businessType, string code, Guid userId);
}
