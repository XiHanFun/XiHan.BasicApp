// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 会话角色激活的调用。
/// </summary>
/// <remarks>
/// Access 实现 <see cref="ISessionRoleActivator"/>。没装即持有的角色全部进令牌。
/// </remarks>
internal static class SessionRoleActivation
{
    /// <summary>
    /// 开会话、切换租户与模仿：重新激活。
    /// </summary>
    public static async Task<IReadOnlyList<string>> StartAsync(this IEnumerable<ISessionRoleActivator> activators, Guid sessionId, Guid userId, string? tenantId, IReadOnlyList<string> heldRoleIds, InvocationContext context)
        => activators.FirstOrDefault() is { } activator
            ? await activator.StartAsync(sessionId, userId, tenantId ?? TenantIds.Platform, heldRoleIds, context).ConfigureAwait(false)
            : heldRoleIds;

    /// <summary>
    /// 续签与结束模仿：沿用并重新核对。
    /// </summary>
    public static async Task<IReadOnlyList<string>> RefreshAsync(this IEnumerable<ISessionRoleActivator> activators, Guid sessionId, Guid userId, string? tenantId, IReadOnlyList<string> heldRoleIds, InvocationContext context)
        => activators.FirstOrDefault() is { } activator
            ? await activator.RefreshAsync(sessionId, userId, tenantId ?? TenantIds.Platform, heldRoleIds, context).ConfigureAwait(false)
            : heldRoleIds;

    /// <summary>
    /// 换激活集。
    /// </summary>
    public static async Task<Result<IReadOnlyList<string>>> ActivateAsync(this IEnumerable<ISessionRoleActivator> activators, Guid sessionId, Guid userId, string? tenantId, IReadOnlyList<string> heldRoleIds, IReadOnlyList<string> requestedRoleIds, InvocationContext context)
        => activators.FirstOrDefault() is { } activator
            ? await activator.ActivateAsync(sessionId, userId, tenantId ?? TenantIds.Platform, heldRoleIds, requestedRoleIds, context).ConfigureAwait(false)
            : Result.Success(heldRoleIds);
}
