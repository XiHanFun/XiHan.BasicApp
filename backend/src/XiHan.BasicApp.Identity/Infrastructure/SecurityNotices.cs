// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
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
/// 经 Contracts 的站内投递发一条安全通知（不看收件人的通知偏好，投递失败只记日志、不挡业务）。文案按收件人的界面语言，没存语言的按发起这次调用的区域
/// 通知落在收件人要去的那一侧：调用不在那一侧时（平台主体模仿租户里的人、匿名的登录调用、退出时通知别的侧）另起一次那一侧的调用，独立提交（调用自己的事务写不到那一侧），所以要在调用第一次写入之前调用，SQLite 上才不会等自己的写锁
/// 收件箱与推送都按侧，提醒别的设备时投到它们各自所在的一侧、每侧一条，那一侧在线的端实时收到
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
    /// 添加通行密钥通知的业务类型
    /// </summary>
    public const string PasskeyAddedBusinessType = "auth.passkey-added";

    /// <summary>
    /// 登录成功通知的业务类型
    /// </summary>
    public const string LoginBusinessType = "auth.login";

    /// <summary>
    /// 账号在其他设备或新设备登录通知的业务类型
    /// </summary>
    public const string LoginElsewhereBusinessType = "auth.login-elsewhere";

    /// <summary>
    /// 账号在其他设备退出登录通知的业务类型
    /// </summary>
    public const string LogoutElsewhereBusinessType = "auth.logout-elsewhere";

    /// <summary>
    /// 管理员强制下线通知的业务类型
    /// </summary>
    public const string SessionRevokedBusinessType = "auth.session-revoked";

    /// <summary>
    /// 密码已修改通知的业务类型
    /// </summary>
    public const string PasswordChangedBusinessType = "auth.password-changed";

    /// <summary>
    /// 用户名已修改通知的业务类型
    /// </summary>
    public const string UserNameChangedBusinessType = "auth.username-changed";

    /// <summary>
    /// 绑定第三方账号通知的业务类型
    /// </summary>
    public const string ExternalLinkedBusinessType = "auth.external-linked";

    /// <summary>
    /// 开放接口凭证变更通知的业务类型
    /// </summary>
    public const string ApiCredentialBusinessType = "auth.api-credential";

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

    /// <summary>
    /// 提醒账号本人密码已过期、要先改密
    /// </summary>
    /// <param name="user">账号</param>
    /// <param name="tenant">登录的上下文；平台为 <see langword="null"/></param>
    /// <param name="days">密码有效天数</param>
    /// <param name="context">调用上下文</param>
    public Task PasswordExpiredAsync(SysUser user, string? tenant, int days, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        return DeliverAsync(user.Id, tenant, IdentityMessages.PasswordExpiredNoticeTitle, IdentityMessages.PasswordExpiredNoticeContent, [("days", days)], ProfileLink, PasswordChangeBusinessType, user.Id, context);
    }

    /// <summary>
    /// 告诉账号本人添加了一个通行密钥
    /// </summary>
    /// <remarks>
    /// 本人在当前上下文里添加，通知落在同一侧、随调用的事务提交
    /// </remarks>
    /// <param name="user">账号</param>
    /// <param name="passkey">添加的通行密钥</param>
    /// <param name="context">调用上下文</param>
    public Task PasskeyAddedAsync(SysUser user, SysUserPasskey passkey, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(passkey);
        return DeliverHereAsync(user.Id, IdentityMessages.PasskeyAddedNoticeTitle, IdentityMessages.PasskeyAddedNoticeContent, [("name", passkey.Name)], PasskeyAddedBusinessType, passkey.Id, context);
    }

    /// <summary>
    /// 告诉账号本人有一次新的登录
    /// </summary>
    /// <remarks>
    /// 别的设备上还登录着：提醒投到那些会话所在的各侧，这台设备以前登录过即「其他设备登录」，否则「新设备登录」；
    /// 别处都没登录着：在新会话这一侧记一条「登录成功」。正文带新登录的浏览器、系统、IP 与地点，业务 ID 是新会话
    /// 在调用第一次写入之前、新会话还没写入时调用
    /// </remarks>
    /// <param name="user">账号</param>
    /// <param name="session">新会话</param>
    /// <param name="knownDevice">这台设备以前登录过这个账号</param>
    /// <param name="others">账号别的设备会话（这次被登录策略挤掉的也在内，它们随后收到强制下线）</param>
    /// <param name="context">调用上下文</param>
    public Task LoginAsync(SysUser user, SysUserSession session, bool knownDevice, IReadOnlyCollection<SysUserSession> others, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(others);
        (string Name, object? Value)[] arguments = [("client", ClientOf(session))];
        if (others.Count == 0)
        {
            return DeliverAsync(user.Id, session.ActiveTenantId, IdentityMessages.LoginNoticeTitle, IdentityMessages.LoginNoticeContent, arguments, ProfileLink, LoginBusinessType, session.Id, context);
        }

        var (title, content) = knownDevice
            ? (IdentityMessages.LoginElsewhereNoticeTitle, IdentityMessages.LoginElsewhereNoticeContent)
            : (IdentityMessages.NewDeviceNoticeTitle, IdentityMessages.NewDeviceNoticeContent);
        return DeliverToSidesAsync(user.Id, others.Select(static s => s.ActiveTenantId), title, content, arguments, LoginElsewhereBusinessType, session.Id, context);
    }

    /// <summary>
    /// 告诉账号本人有一台设备退出了登录
    /// </summary>
    /// <remarks>
    /// 投到退出的这一侧与账号别的设备会话所在的各侧，正文带退出的那台设备；推送按记下的顺序发出，退出的这台设备的实时连接要先断开，否则它自己也会弹出这条提醒
    /// 在调用第一次写入之前调用
    /// </remarks>
    /// <param name="session">退出的会话</param>
    /// <param name="others">账号别的设备会话</param>
    /// <param name="context">调用上下文</param>
    public Task LogoutAsync(SysUserSession session, IReadOnlyCollection<SysUserSession> others, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(others);
        return DeliverToSidesAsync(session.UserId, [session.ActiveTenantId, .. others.Select(static s => s.ActiveTenantId)], IdentityMessages.LogoutNoticeTitle, IdentityMessages.LogoutNoticeContent, [("client", ClientOf(session))], LogoutElsewhereBusinessType, session.Id, context);
    }

    /// <summary>
    /// 告诉账号本人管理员强制下线了他的一个会话
    /// </summary>
    /// <remarks>
    /// 被下线的端先收到强制下线并断开，这条留在那一侧的收件箱里，同一侧别的端实时收到；正文带那台设备与管理员填的原因
    /// </remarks>
    /// <param name="session">被下线的会话（与调用在同一侧）</param>
    /// <param name="reason">管理员填的原因；没填为 <see langword="null"/></param>
    /// <param name="context">调用上下文</param>
    public Task SessionRevokedAsync(SysUserSession session, string? reason, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(session);
        return reason is null
            ? DeliverAsync(session.UserId, session.ActiveTenantId, IdentityMessages.SessionRevokedNoticeTitle, IdentityMessages.SessionRevokedNoticeContent, [("client", ClientOf(session))], ProfileLink, SessionRevokedBusinessType, session.Id, context)
            : DeliverAsync(session.UserId, session.ActiveTenantId, IdentityMessages.SessionRevokedNoticeTitle, IdentityMessages.SessionRevokedNoticeContentWithReason, [("client", ClientOf(session)), ("reason", reason)], ProfileLink, SessionRevokedBusinessType, session.Id, context);
    }

    /// <summary>
    /// 告诉账号本人密码改了
    /// </summary>
    /// <param name="user">账号</param>
    /// <param name="context">调用上下文</param>
    public Task PasswordChangedAsync(SysUser user, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        return DeliverHereAsync(user.Id, IdentityMessages.PasswordChangedNoticeTitle, IdentityMessages.PasswordChangedNoticeContent, [], PasswordChangedBusinessType, user.Id, context);
    }

    /// <summary>
    /// 告诉账号本人用户名改了
    /// </summary>
    /// <param name="user">改过用户名的账号</param>
    /// <param name="context">调用上下文</param>
    public Task UserNameChangedAsync(SysUser user, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        return DeliverHereAsync(user.Id, IdentityMessages.UserNameChangedNoticeTitle, IdentityMessages.UserNameChangedNoticeContent, [("userName", user.UserName)], UserNameChangedBusinessType, user.Id, context);
    }

    /// <summary>
    /// 告诉账号本人绑定了一个第三方账号
    /// </summary>
    /// <param name="binding">绑定</param>
    /// <param name="provider">第三方登录方式的名字</param>
    /// <param name="context">调用上下文</param>
    public Task ExternalLinkedAsync(SysUserExternalLogin binding, string provider, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(binding);
        return DeliverHereAsync(binding.UserId, IdentityMessages.ExternalLinkedNoticeTitle, IdentityMessages.ExternalLinkedNoticeContent, [("provider", provider)], ExternalLinkedBusinessType, binding.Id, context);
    }

    /// <summary>
    /// 告诉账号本人新建了一个开放接口凭证
    /// </summary>
    /// <param name="credential">凭证</param>
    /// <param name="context">调用上下文</param>
    public Task ApiCredentialCreatedAsync(SysUserApiCredential credential, InvocationContext context)
        => ApiCredentialAsync(credential, IdentityMessages.ApiCredentialCreatedNoticeTitle, IdentityMessages.ApiCredentialCreatedNoticeContent, context);

    /// <summary>
    /// 告诉账号本人滚动了一个开放接口凭证的密钥
    /// </summary>
    /// <param name="credential">凭证</param>
    /// <param name="context">调用上下文</param>
    public Task ApiCredentialRotatedAsync(SysUserApiCredential credential, InvocationContext context)
        => ApiCredentialAsync(credential, IdentityMessages.ApiCredentialRotatedNoticeTitle, IdentityMessages.ApiCredentialRotatedNoticeContent, context);

    /// <summary>
    /// 告诉账号本人删除了一个开放接口凭证
    /// </summary>
    /// <param name="credential">凭证</param>
    /// <param name="context">调用上下文</param>
    public Task ApiCredentialDeletedAsync(SysUserApiCredential credential, InvocationContext context)
        => ApiCredentialAsync(credential, IdentityMessages.ApiCredentialDeletedNoticeTitle, IdentityMessages.ApiCredentialDeletedNoticeContent, context);

    // 浏览器、系统、IP 与地点，都认不出时按收件人的语言写「未知设备」
    private static object ClientOf(SysUserSession session)
    {
        var client = string.Join(" · ", new[] { session.Browser, session.Os, session.IpAddress, session.Location }.Where(static part => !string.IsNullOrWhiteSpace(part)));
        return client.Length == 0 ? new Phrase(IdentityMessages.UnknownClient) : client;
    }

    private Task ApiCredentialAsync(SysUserApiCredential credential, string titleKey, string contentKey, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return DeliverHereAsync(credential.UserId, titleKey, contentKey, [("name", credential.Name), ("appKey", credential.AppKey)], ApiCredentialBusinessType, credential.Id, context);
    }

    // 本人在当前上下文里做的事：通知落在同一侧、随调用的事务提交
    private Task DeliverHereAsync(Guid userId, string titleKey, string contentKey, (string Name, object? Value)[] arguments, string businessType, Guid businessId, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return DeliverAsync(userId, context.GetSecurity().Tenant?.Id, titleKey, contentKey, arguments, ProfileLink, businessType, businessId, context);
    }

    // 每侧一条；调用所在的一侧排最后：它随调用的事务写入，之后再起的别侧调用在 SQLite 上会等这把写锁
    private async Task DeliverToSidesAsync(Guid userId, IEnumerable<string?> sides, string titleKey, string contentKey, (string Name, object? Value)[] arguments, string businessType, Guid businessId, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var current = context.GetSecurity().Tenant?.Id;
        foreach (var side in sides.Distinct(StringComparer.Ordinal).OrderBy(side => string.Equals(side, current, StringComparison.Ordinal)))
        {
            await DeliverAsync(userId, side, titleKey, contentKey, arguments, ProfileLink, businessType, businessId, context).ConfigureAwait(false);
        }
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
            : await runner.RunAsync(tenant ?? TenantIds.Platform, ctx => new ValueTask<Result>(DeliverToAsync(ctx.Services.GetServices<IInboxDelivery>(), ctx, message)), context.CancellationToken).ConfigureAwait(false);
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

    // 键都在 10 种区域里，取不到是缺陷，直接抛出；参数是 Phrase 的先按同一区域译出
    private string Render(string locale, string key, (string Name, object? Value)[] arguments)
    {
        var values = arguments.ToDictionary(static a => a.Name, a => a.Value is Phrase phrase ? Render(locale, phrase.Key, []) : a.Value, StringComparer.Ordinal);
        return localizer.Resolve(key, locale, values).TryGetValue(out var text)
            ? text.Text
            : throw new InvalidOperationException($"Identity text '{key}' is missing for locale '{locale}'.");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The {BusinessType} notice was not delivered ({Code}) to {UserId}; the caller goes on.")]
    private static partial void LogNotDelivered(ILogger logger, string businessType, string code, Guid userId);

    // 要按收件人的语言译出的参数
    private sealed record Phrase(string Key);
}
