// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 登录时的访问策略检查
/// </summary>
/// <remarks>
/// 密码、两步验证、邮箱、手机号、第三方登录与切换租户都经过这里。被拒绝的登录记为 <see cref="LoginResult.AccessDenied"/>，不计入自动封禁。
/// 求值本身出错时记错误日志并放行。
/// </remarks>
public sealed class AccessPolicyLoginGuard : IAccessPolicyLoginGuard
{
    /// <summary>
    /// 被拒绝时的说明
    /// </summary>
    public const string DeniedMessage = "当前网络或时段不允许登录。";

    private readonly IOptionsMonitor<AccessPolicyOptions> _options;
    private readonly AccessPolicyAuthenticatedEvaluator _evaluator;
    private readonly IAccessPolicyHitRecorder _hits;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalEventBus _localEventBus;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly ITraceIdProvider _traceIdProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccessPolicyLoginGuard> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyLoginGuard(
        IOptionsMonitor<AccessPolicyOptions> options,
        AccessPolicyAuthenticatedEvaluator evaluator,
        IAccessPolicyHitRecorder hits,
        IHttpContextAccessor httpContextAccessor,
        ILocalEventBus localEventBus,
        IClientInfoProvider clientInfoProvider,
        ITraceIdProvider traceIdProvider,
        TimeProvider timeProvider,
        ILogger<AccessPolicyLoginGuard> logger)
    {
        _options = options;
        _evaluator = evaluator;
        _hits = hits;
        _httpContextAccessor = httpContextAccessor;
        _localEventBus = localEventBus;
        _clientInfoProvider = clientInfoProvider;
        _traceIdProvider = traceIdProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnsureLoginAllowedAsync(long userId, string? userName, long? tenantId, CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        AccessPolicyDecision? decision;
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            decision = await _evaluator.EvaluateAsync(
                new SaasConditionContext
                {
                    TenantId = tenantId ?? 0,
                    Now = now,
                    UserId = userId,
                    UserName = userName,
                    HttpContext = httpContext,
                    PermissionCodes = []
                },
                AccessPolicyRequests.IsPlatformAllowed(httpContext),
                cancellationToken);
            if (decision is not null)
            {
                AccessPolicyRequests.Record(_hits, decision, AccessPolicyPhase.Login, now, AccessPolicyRequests.Describe(httpContext, null, userId, userName, withClient: false));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "登录时的访问策略求值失败，本次放行");
            return;
        }

        if (decision is not { IsBlocked: true })
        {
            return;
        }

        var client = _clientInfoProvider.GetCurrent();
        await _localEventBus.PublishAsync(new AuthLoginFailedDomainEvent(
            tenantId,
            userId,
            userName,
            LoginResult.AccessDenied,
            DeniedMessage,
            now,
            _traceIdProvider.GetCurrentTraceId(),
            client.IpAddress,
            client.UserAgent));
        throw new InvalidOperationException(DeniedMessage);
    }
}
