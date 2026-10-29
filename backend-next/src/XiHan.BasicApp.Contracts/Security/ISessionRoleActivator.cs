// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 会话的角色激活（Access 实现）。
/// </summary>
/// <remarks>
/// Identity 在每个签发令牌的地方调用；没有实现即持有的角色全部进令牌。
/// 令牌只带激活的角色；同一动态职责分离（DSD）组里的角色同一会话只能激活允许的个数，按角色排序取最前的。
/// </remarks>
public interface ISessionRoleActivator
{
    /// <summary>
    /// 开会话、切换租户与模仿登录时重排这一侧的激活集。
    /// </summary>
    /// <remarks>
    /// 这一侧的激活集重新来过——不冲突的角色全部激活，同一 DSD 组只激活排序最前的。
    /// </remarks>
    /// <param name="sessionId">会话。</param>
    /// <param name="userId">用户。</param>
    /// <param name="tenantId">会话的一侧（平台为 <see cref="TenantIds.Platform"/>）。</param>
    /// <param name="heldRoleIds">此刻持有的角色 ID。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>激活的角色 ID（进令牌）。</returns>
    Task<IReadOnlyList<string>> StartAsync(Guid sessionId, Guid userId, string tenantId, IReadOnlyList<string> heldRoleIds, InvocationContext context);

    /// <summary>
    /// 续签时重新核对会话的激活集。
    /// </summary>
    /// <remarks>
    /// 沿用会话的激活集，按此刻持有与当前的 DSD 规则重新核对——不再持有的记到期，冲突的按同一排序只留一个、其余记停用，新持有的不冲突即激活。
    /// </remarks>
    /// <param name="sessionId">会话。</param>
    /// <param name="userId">用户。</param>
    /// <param name="tenantId">会话的一侧。</param>
    /// <param name="heldRoleIds">此刻持有的角色 ID。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>激活的角色 ID（进令牌）。</returns>
    Task<IReadOnlyList<string>> RefreshAsync(Guid sessionId, Guid userId, string tenantId, IReadOnlyList<string> heldRoleIds, InvocationContext context);

    /// <summary>
    /// 换激活集（本人在 <c>auth.activate-roles</c>）。
    /// </summary>
    /// <remarks>
    /// 要激活的须都此刻持有，且合起来不违反 DSD；成功后其余持有的记停用。
    /// </remarks>
    /// <param name="sessionId">会话。</param>
    /// <param name="userId">用户。</param>
    /// <param name="tenantId">会话的一侧。</param>
    /// <param name="heldRoleIds">此刻持有的角色 ID。</param>
    /// <param name="requestedRoleIds">要激活的角色 ID（至少一个）。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>激活的角色 ID（进令牌）；不持有或违反 DSD 即失败。</returns>
    Task<Result<IReadOnlyList<string>>> ActivateAsync(Guid sessionId, Guid userId, string tenantId, IReadOnlyList<string> heldRoleIds, IReadOnlyList<string> requestedRoleIds, InvocationContext context);
}
