// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Middleware;

/// <summary>
/// 访问策略的判定（Tenant 阶段）。
/// </summary>
/// <remarks>
/// 排在租户解析之后：认证与租户都已知。
/// 先求平台层（管全部调用），平台层拦下即不看租户层；
/// 再求已登录租户调用的租户层。层内有放行命中即不拒绝，否则第一条命中的拒绝策略拦下（403 <c>ACCESS_DENIED_004</c>）。
/// 每条命中都记一条条件命中（平台层的落在平台一侧），时机按策略引用的属性推出认证前 / 认证后。
/// 4.x 分认证前、认证后两个中间件；6.0 认证在 Tenant 阶段之前，合成一个，认证失败的调用不再经过策略。判定本身出错时放行并记警告（4.x 同）。
/// 策略之前先看登录失败自动封禁（<see cref="LoginBans"/>）：来源地址封禁中即 503 <c>ACCESS_UNAVAILABLE_001</c>（带重试秒数），记一条编码 <c>login-auto-ban</c> 的命中。
/// 平台层有放行命中时在调用上下文里留标记（<see cref="PlatformAllowedKey"/>），这次调用的认证失败不计入封禁（4.x 同）。
/// </remarks>
/// <param name="policies">策略缓存。</param>
/// <param name="evaluator">策略求值。</param>
/// <param name="bans">登录失败自动封禁。</param>
/// <param name="options">授权包配置。</param>
/// <param name="security">安全配置（角色声明名）。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class AccessPolicyMiddleware(AccessPolicies policies, AccessPolicyEvaluator evaluator, LoginBans bans, AccessOptions options, SecurityOptions security, TimeProvider clock, ILogger<AccessPolicyMiddleware> logger) : IPipelineMiddleware
{
    /// <summary>
    /// 自动封禁命中的编码。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public const string AutoBanCode = "login-auto-ban";

    /// <summary>
    /// 调用上下文里「平台层放行了这次调用」的标记键。
    /// </summary>
    public static readonly object PlatformAllowedKey = new();

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        if (await BannedSecondsAsync(context).ConfigureAwait(false) is { } retryAfter)
        {
            context.AddAuditLog(new ConditionHitLogEntry(ConditionHitSource.AccessPolicy, AutoBanCode)
            {
                TenantId = TenantIds.Platform,
                Action = ConditionHitAction.Deny,
                Phase = ConditionHitPhase.BeforeAuthentication,
                IsBlocked = true,
            });
            context.SetOutcome(Error.Unavailable(AccessErrors.LoginBanned, AccessMessages.LoginBanned, MessageArguments.Of(("retryAfter", retryAfter))));
            return;
        }

        if (!options.AccessPolicyEnabled)
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        AccessPolicyView? blockedBy;
        try
        {
            blockedBy = await DecideAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogEvaluationFailed(logger, exception);
            blockedBy = null;
        }

        if (blockedBy is not null)
        {
            context.SetOutcome(Error.Denied(AccessErrors.PolicyDenied, AccessMessages.PolicyDenied));
            return;
        }

        await nextStep(context).ConfigureAwait(false);
    }

    private async Task<AccessPolicyView?> DecideAsync(InvocationContext context)
    {
        var cancellationToken = context.CancellationToken;
        var now = clock.GetUtcNow();
        var request = AccessRequests.Current(context, security, now);
        IConditionAttributeSource? source = null;

        var platform = await policies.OfAsync(TenantIds.Platform, cancellationToken).ConfigureAwait(false);
        if (platform.Count > 0)
        {
            source = ConditionAttributeSource.Create(request, context.Services, options);
            var layer = await evaluator.EvaluateAsync(platform, source, now, platform: true, cancellationToken).ConfigureAwait(false);
            Record(context, layer, request, TenantIds.Platform);
            if (layer.BlockedBy is not null)
            {
                return layer.BlockedBy;
            }

            if (layer.Allowed)
            {
                context.Items[PlatformAllowedKey] = true;
            }
        }

        var owner = RoleService.OwnerOf(context);
        if (!context.GetSecurity().Principal.IsAuthenticated || string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal))
        {
            return null;
        }

        var own = await policies.OfAsync(owner, cancellationToken).ConfigureAwait(false);
        if (own.Count == 0)
        {
            return null;
        }

        source ??= ConditionAttributeSource.Create(request, context.Services, options);
        var tenantLayer = await evaluator.EvaluateAsync(own, source, now, platform: false, cancellationToken).ConfigureAwait(false);
        Record(context, tenantLayer, request, null);
        return tenantLayer.BlockedBy;
    }

    // 来源地址封禁中时还剩几秒；内部调用没有地址，不查。查不了（缓存故障）时放行并记警告
    private async Task<int?> BannedSecondsAsync(InvocationContext context)
    {
        if (!context.GetOrigin().TryGetValue(out var origin) || string.IsNullOrWhiteSpace(origin.RemoteAddress))
        {
            return null;
        }

        try
        {
            return await bans.BannedUntilAsync(origin.RemoteAddress, context.CancellationToken).ConfigureAwait(false) is { } until
                ? (int)Math.Max(1, Math.Ceiling((until - clock.GetUtcNow()).TotalSeconds))
                : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogEvaluationFailed(logger, exception);
            return null;
        }
    }

    // 只有与拦截策略同层的拒绝记为拦截（4.x 同）
    private static void Record(InvocationContext context, AccessPolicyLayer layer, ConditionRequest request, string? tenantId)
    {
        foreach (var hit in layer.Hits)
        {
            context.AddAuditLog(new ConditionHitLogEntry(ConditionHitSource.AccessPolicy, hit.Policy.Code)
            {
                TenantId = tenantId,
                PolicyId = hit.Policy.Id,
                PolicyName = hit.Policy.Name,
                Action = ActionOf(hit.Policy.Action),
                Phase = hit.Phase,
                IsBlocked = layer.BlockedBy is not null && hit.Policy.Action == AccessPolicyAction.Deny,
                PermissionCodes = request.PermissionCodes,
            });
        }
    }

    /// <summary>
    /// 策略动作换成命中记录的动作。
    /// </summary>
    public static ConditionHitAction ActionOf(AccessPolicyAction action) => action switch
    {
        AccessPolicyAction.Deny => ConditionHitAction.Deny,
        AccessPolicyAction.Allow => ConditionHitAction.Allow,
        _ => ConditionHitAction.LogOnly,
    };

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Access policy evaluation failed; the call is let through.")]
    private static partial void LogEvaluationFailed(ILogger logger, Exception exception);
}
