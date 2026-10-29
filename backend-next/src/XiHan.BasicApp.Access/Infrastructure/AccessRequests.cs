// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 调用上下文 → 判定用的请求事实（权限条件与访问策略共用）：HTTP 的请求目标是「方法 路径」，其他协议只有目标（gRPC 方法全名、MCP 工具名等）；
/// 客户端类型未登录为 <c>anonymous</c>，否则为调用协议。
/// </summary>
public static class AccessRequests
{
    /// <summary>
    /// 组装。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="roleIds">此刻持有的角色（访问策略取令牌声明，权限条件取判定所用的角色）。</param>
    /// <param name="permissionCodes">本次调用要求的权限码。</param>
    /// <param name="operationId">操作 ID。</param>
    /// <param name="now">判定时刻。</param>
    public static ConditionRequest From(InvocationContext context, IReadOnlyList<Guid> roleIds, IReadOnlyList<string> permissionCodes, string? operationId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        var (origin, method, path) = TargetOf(context);
        var authenticated = security.Principal.IsAuthenticated;
        return new ConditionRequest(
            security.Tenant?.Id ?? TenantIds.Platform,
            authenticated && Guid.TryParse(security.Principal.Id, out var userId) ? userId : null,
            security.Principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault(),
            security.Principal.ClaimValues(IdentityClaims.Actor).Any(),
            roleIds,
            permissionCodes,
            path,
            method,
            operationId,
            origin?.RemoteAddress,
            origin?.UserAgent,
            authenticated ? origin?.Protocol : "anonymous",
            now);
    }

    /// <summary>
    /// 登录时机看到的这次调用：主体取即将签发令牌的账号（调用本身还是匿名的），没有权限码，客户端类型为调用协议。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="attempt">这次登录。</param>
    /// <param name="now">判定时刻。</param>
    public static ConditionRequest ForLogin(InvocationContext context, LoginAttempt attempt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attempt);
        var (origin, method, path) = TargetOf(context);
        var operation = context.GetOperation().TryGetValue(out var found) ? found : null;
        return new ConditionRequest(
            attempt.TenantId,
            attempt.UserId,
            attempt.UserName,
            false,
            [.. attempt.RoleIds.Select(static v => Guid.TryParse(v, out var id) ? id : (Guid?)null).OfType<Guid>().Distinct()],
            [],
            path,
            method,
            operation?.Id,
            origin?.RemoteAddress,
            origin?.UserAgent,
            origin?.Protocol,
            now);
    }

    /// <summary>
    /// 访问策略看到的这次调用：角色取令牌声明，权限码取操作契约声明的那一个。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="options">安全配置（角色声明名）。</param>
    /// <param name="now">判定时刻。</param>
    public static ConditionRequest Current(InvocationContext context, SecurityOptions options, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);
        var operation = context.GetOperation().TryGetValue(out var found) ? found : null;
        return From(context, ClaimedRoles(context, options), operation?.Contract.Permission is { } permission ? [permission] : [], operation?.Id, now);
    }

    // HTTP 的请求目标是「方法 路径」，其他协议只有目标
    private static (InvocationOrigin? Origin, string? Method, string? Path) TargetOf(InvocationContext context)
    {
        var origin = context.GetOrigin().TryGetValue(out var found) ? found : null;
        if (origin is not null && origin.Protocol == InvocationProtocols.Http && origin.Target.IndexOf(' ', StringComparison.Ordinal) is var space and > 0)
        {
            return (origin, origin.Target[..space], origin.Target[(space + 1)..]);
        }

        return (origin, null, origin?.Target);
    }

    /// <summary>
    /// 令牌里的角色声明（访问策略用：不必为每次调用组装授予全貌）。
    /// </summary>
    public static IReadOnlyList<Guid> ClaimedRoles(InvocationContext context, SecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        return [.. context.GetSecurity().Principal.ClaimValues(options.RoleClaim).Select(static v => Guid.TryParse(v, out var id) ? id : (Guid?)null).OfType<Guid>().Distinct()];
    }
}
