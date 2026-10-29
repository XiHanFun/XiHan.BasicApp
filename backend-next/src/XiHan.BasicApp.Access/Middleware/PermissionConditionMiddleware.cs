// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Middleware;

/// <summary>
/// 权限条件的调用时判定（Authorize 阶段，排在框架授权之后）：框架的权限过滤器拿不到请求属性，持有的权限码上挂了条件时，
/// 这里按调用来源（地址、User-Agent、路径与方法）、主体与时刻精确判定操作声明的权限码；不满足时 <see cref="PermissionConditionMode.Enforce"/> 拒绝、
/// <see cref="PermissionConditionMode.LogOnly"/> 放行，两者都记一条条件命中。只判定操作契约声明的权限码（4.x 在权限检查器里判定任何检查）。
/// </summary>
/// <param name="evaluator">授予全貌。</param>
/// <param name="decider">条件判定。</param>
/// <param name="options">授权包配置。</param>
/// <param name="clock">时间源。</param>
public sealed class PermissionConditionMiddleware(GrantEvaluator evaluator, ConditionDecider decider, AccessOptions options, TimeProvider clock) : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        var security = context.GetSecurity();
        if (options.PermissionConditionMode == PermissionConditionMode.Disabled
            || !security.Principal.IsAuthenticated
            || !context.GetOperation().TryGetValue(out var operation)
            || operation.Contract.Permission is not { } permission
            || await evaluator.ViewAsync(security, context.CancellationToken).ConfigureAwait(false) is not { } view
            || !view.RequiresConditions(permission))
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        var source = ConditionAttributeSource.Create(RequestOf(context, security, view, permission, operation.Id), context.Services, options);
        var decision = await decider.DecideAsync(view, permission, source, explain: false, context.CancellationToken).ConfigureAwait(false);
        if (decision.Verdict is ConditionVerdict.Granted or ConditionVerdict.Unconditional)
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        var enforce = options.PermissionConditionMode == PermissionConditionMode.Enforce;
        context.AddAuditLog(new ConditionHitLogEntry(ConditionHitSource.PermissionCondition, permission)
        {
            Action = enforce ? ConditionHitAction.Deny : ConditionHitAction.LogOnly,
            Phase = ConditionHitPhase.Authorization,
            IsBlocked = enforce,
            PermissionCodes = [permission],
        });
        if (enforce)
        {
            context.SetOutcome(Error.Denied(AccessErrors.ConditionNotMet, AccessMessages.ConditionNotMet, MessageArguments.Of(("permission", permission))));
            return;
        }

        await nextStep(context).ConfigureAwait(false);
    }

    /// <summary>
    /// 调用上下文 → 请求事实：HTTP 的请求目标是「方法 路径」，其他协议只有目标（gRPC 方法全名、MCP 工具名等）。
    /// </summary>
    private ConditionRequest RequestOf(InvocationContext context, SecurityContext security, GrantView view, string permission, string operationId)
    {
        var origin = context.GetOrigin().TryGetValue(out var found) ? found : null;
        string? method = null;
        var path = origin?.Target;
        if (origin is not null && origin.Protocol == InvocationProtocols.Http && origin.Target.IndexOf(' ', StringComparison.Ordinal) is var space and > 0)
        {
            method = origin.Target[..space];
            path = origin.Target[(space + 1)..];
        }

        return new ConditionRequest(
            security.Tenant?.Id ?? TenantIds.Platform,
            Guid.TryParse(security.Principal.Id, out var userId) ? userId : null,
            security.Principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault(),
            security.Principal.ClaimValues(IdentityClaims.Actor).Any(),
            [.. view.Roles.Select(static r => r.RoleId)],
            [permission],
            path,
            method,
            operationId,
            origin?.RemoteAddress,
            origin?.UserAgent,
            origin?.Protocol,
            clock.GetUtcNow());
    }
}
