// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
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
        var origin = context.GetOrigin().TryGetValue(out var found) ? found : null;
        string? method = null;
        var path = origin?.Target;
        if (origin is not null && origin.Protocol == InvocationProtocols.Http && origin.Target.IndexOf(' ', StringComparison.Ordinal) is var space and > 0)
        {
            method = origin.Target[..space];
            path = origin.Target[(space + 1)..];
        }

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
