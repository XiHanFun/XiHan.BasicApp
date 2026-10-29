// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Net;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.LoginBans;

/// <summary>
/// <c>login-bans.list</c> 的请求（无参数）。
/// </summary>
public sealed record LoginBansRequest;

/// <summary>
/// 登录失败自动封禁此刻生效的参数。
/// </summary>
/// <param name="ConfigKey">系统参数键（在参数配置里改）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="WindowMinutes">计数窗口（分钟）。</param>
/// <param name="MaxFailures">失败次数上限。</param>
/// <param name="BanMinutes">首次封禁（分钟）。</param>
/// <param name="MaxBanMinutes">封禁上限（分钟）。</param>
/// <param name="Error">参数写错时的原因（按请求区域翻译；此时按缺省值显示、不计数）。</param>
public sealed record LoginAutoBanSettingsResponse(string ConfigKey, bool IsEnabled, int WindowMinutes, int MaxFailures, int BanMinutes, int MaxBanMinutes, string? Error);

/// <summary>
/// 一条封禁。
/// </summary>
/// <param name="Ip">地址。</param>
/// <param name="Until">解封时刻。</param>
/// <param name="RemainingSeconds">还剩几秒。</param>
public sealed record LoginBanResponse(string Ip, DateTimeOffset Until, int RemainingSeconds);

/// <summary>
/// <c>login-bans.list</c> 的响应。
/// </summary>
/// <param name="Settings">此刻生效的参数。</param>
/// <param name="Items">当前封禁，按解封时刻排列。</param>
public sealed record LoginBansResponse(LoginAutoBanSettingsResponse Settings, IReadOnlyList<LoginBanResponse> Items);

/// <summary>
/// <c>login-bans.lift</c> 的请求。
/// </summary>
/// <param name="Ip">要解封的地址。</param>
public sealed record LiftLoginBanRequest([Required, StringLength(64, MinimumLength = 1)] string Ip);

/// <summary>
/// <c>login-bans.lift</c> 的响应。
/// </summary>
/// <param name="Ip">已解封的地址。</param>
public sealed record LiftLoginBanResponse(string Ip);

/// <summary>
/// 登录失败自动封禁的参数与当前封禁：<c>GET /api/login-bans</c>（只在平台，4.x 同）。参数在系统参数 <c>access.security.login-auto-ban</c>，缺省关。
/// </summary>
/// <param name="bans">封禁存储。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.LoginBansRead)]
public sealed class ListLoginBans(Infrastructure.LoginBans bans, TimeProvider clock) : IOperation<LoginBansRequest, LoginBansResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "login-bans.list";

    /// <inheritdoc />
    public async ValueTask<Result<LoginBansResponse>> HandleAsync(LoginBansRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var configured = await LoginDefense.SettingsAsync(context).ConfigureAwait(false);
        var settings = configured.TryGetValue(out var current) ? current : LoginAutoBanSettings.Default;
        var now = clock.GetUtcNow();
        var items = (await bans.ListAsync(context.CancellationToken).ConfigureAwait(false))
            .Select(b => new LoginBanResponse(b.Ip, b.Until, (int)Math.Max(1, Math.Ceiling((b.Until - now).TotalSeconds))))
            .ToList();
        return new LoginBansResponse(
            new LoginAutoBanSettingsResponse(LoginAutoBanSettings.ConfigKey, settings.IsEnabled, settings.WindowMinutes, settings.MaxFailures, settings.BanMinutes, settings.MaxBanMinutes, configured.IsFailure ? context.Localize(configured.Error!) : null),
            items);
    }
}

/// <summary>
/// 解封一个地址：<c>POST /api/login-bans:lift</c>（只在平台），连同它这一窗口的失败计数；没封着即 <c>ACCESS_NOTFOUND_009</c>。
/// </summary>
/// <param name="bans">封禁存储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.LoginBansLift)]
public sealed class LiftLoginBan(Infrastructure.LoginBans bans) : IOperation<LiftLoginBanRequest, LiftLoginBanResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "login-bans.lift";

    /// <inheritdoc />
    public async ValueTask<Result<LiftLoginBanResponse>> HandleAsync(LiftLoginBanRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ip = request.Ip.Trim();
        if (!IPAddress.TryParse(ip, out _))
        {
            return Error.Validation(AccessErrors.TrialIpInvalid, AccessMessages.TrialIpInvalid, MessageArguments.Of(("ip", ip)));
        }

        var configured = await LoginDefense.SettingsAsync(context).ConfigureAwait(false);
        var lifted = await bans.LiftAsync(ip, configured.TryGetValue(out var current) ? current : LoginAutoBanSettings.Default, context.CancellationToken).ConfigureAwait(false);
        if (!lifted)
        {
            return Error.NotFound(AccessErrors.LoginBanNotFound, AccessMessages.LoginBanNotFound, MessageArguments.Of(("ip", ip)));
        }

        return new LiftLoginBanResponse(ip);
    }
}
