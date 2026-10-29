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
/// 访问策略的判定（Tenant 阶段，排在租户解析之后：认证与租户都已知）：先求平台层（管全部调用），平台层拦下即不看租户层；
/// 再求已登录租户调用的租户层。层内有放行命中即不拒绝，否则第一条命中的拒绝策略拦下（403 <c>ACCESS_DENIED_004</c>）。
/// 每条命中都记一条条件命中（平台层的落在平台一侧），时机按策略引用的属性推出认证前 / 认证后。
/// 4.x 分认证前、认证后两个中间件；6.0 认证在 Tenant 阶段之前，合成一个，认证失败的调用不再经过策略。判定本身出错时放行并记警告（4.x 同）。
/// </summary>
/// <param name="policies">策略缓存。</param>
/// <param name="evaluator">策略求值。</param>
/// <param name="options">授权包配置。</param>
/// <param name="security">安全配置（角色声明名）。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class AccessPolicyMiddleware(AccessPolicies policies, AccessPolicyEvaluator evaluator, AccessOptions options, SecurityOptions security, TimeProvider clock, ILogger<AccessPolicyMiddleware> logger) : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
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
