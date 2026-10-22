// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 登录失败自动封禁：参数取平台的 <see cref="SaasConfigKeys.Security.LoginAutoBan"/>
/// </summary>
/// <remarks>
/// 认证前已命中平台放行策略的请求不计数；24 小时内再次被封时长翻倍，不超过上限。
/// 总开关 <see cref="AccessPolicyOptions.Enabled"/> 只管拦截，计数照常进行。
/// </remarks>
public sealed class LoginBanService : ILoginBanService
{
    private readonly ILoginBanStore _store;
    private readonly LoginBanCheckCache _checkCache;
    private readonly ISaasConfigurationService _configuration;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<AccessPolicyOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LoginBanService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoginBanService(
        ILoginBanStore store,
        LoginBanCheckCache checkCache,
        ISaasConfigurationService configuration,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<AccessPolicyOptions> options,
        TimeProvider timeProvider,
        ILogger<LoginBanService> logger)
    {
        _store = store;
        _checkCache = checkCache;
        _configuration = configuration;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetBanAsync(string ip, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        if (_checkCache.IsKnownNotBanned(ip))
        {
            return null;
        }

        var until = await _store.GetBanExpirationAsync(ip, _timeProvider.GetUtcNow(), cancellationToken);
        if (until is null)
        {
            _checkCache.MarkNotBanned(ip, TimeSpan.FromSeconds(Math.Max(1, _options.CurrentValue.BanCheckCacheSeconds)));
        }

        return until;
    }

    /// <inheritdoc />
    public async Task RecordFailureAsync(string ip, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        if (AccessPolicyRequests.IsPlatformAllowed(_httpContextAccessor.HttpContext))
        {
            return;
        }

        SaasLoginAutoBanSettings settings;
        using (_currentTenant.Change(null))
        {
            settings = await _configuration.GetJsonAsync(SaasConfigKeys.Security.LoginAutoBan, new SaasLoginAutoBanSettings(), cancellationToken);
        }

        if (!settings.IsEnabled || settings.Validate() is not null)
        {
            return;
        }

        var count = await _store.IncrementFailureAsync(ip, TimeSpan.FromMinutes(settings.WindowMinutes), cancellationToken);
        if (count < settings.MaxFailures)
        {
            return;
        }

        var maximum = TimeSpan.FromMinutes(settings.MaxBanMinutes);
        var last = await _store.GetLastBanDurationAsync(ip, cancellationToken);
        var duration = last is { } previous
            ? TimeSpan.FromTicks(Math.Min(previous.Ticks * 2, maximum.Ticks))
            : TimeSpan.FromMinutes(settings.BanMinutes);
        var until = _timeProvider.GetUtcNow() + duration;
        await _store.BanAsync(ip, until, duration, cancellationToken);
        _checkCache.Forget(ip);
        _logger.LogWarning("IP {Ip} 在 {Window} 分钟内登录失败 {Count} 次，封禁 {Minutes} 分钟至 {Until}", ip, settings.WindowMinutes, count, duration.TotalMinutes, until);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LoginBanEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _store.ListAsync(_timeProvider.GetUtcNow(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UnbanAsync(string ip, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        var banned = await _store.UnbanAsync(ip.Trim(), _timeProvider.GetUtcNow(), cancellationToken);
        _checkCache.Forget(ip.Trim());
        return banned;
    }
}
