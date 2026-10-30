// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// 租户成员关系的查询契约。
/// </summary>
/// <remarks>
/// 用户除了注册地租户，还能以成员身份进入别的租户：普通成员是租户内的用户（能切换进去、能在其中被模仿、算租户的收件人），
/// 运维支持成员是平台账号以专门身份进入（能切换进去，但不算租户内的用户）。成员关系只在生效期内有效。
/// 由 Tenancy 实现、Identity 消费；注册地不算成员关系，由调用方另判。没有任何实现时视为没有成员关系、也没有所有者。
/// </remarks>
public interface ITenantMembershipResolver
{
    /// <summary>
    /// <paramref name="userId"/> 能否以成员身份进入 <paramref name="tenantId"/>。
    /// </summary>
    /// <remarks>
    /// 普通成员与运维支持成员都算，须在生效期内。
    /// </remarks>
    Task<bool> CanEnterAsync(string tenantId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="userId"/> 是否是 <paramref name="tenantId"/> 的普通成员。
    /// </summary>
    /// <remarks>
    /// 只算生效期内的普通成员，不含运维支持成员。
    /// </remarks>
    Task<bool> IsMemberAsync(string tenantId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="tenantId"/> 的普通成员用户 ID。
    /// </summary>
    /// <remarks>
    /// 只含生效期内的普通成员：不含注册地在该租户的用户，也不含运维支持成员。
    /// </remarks>
    Task<IReadOnlyList<Guid>> MembersOfAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="userIds"/> 里是某个租户所有者的用户。
    /// </summary>
    /// <remarks>
    /// 所有者不能被删除、锁定或移出成员，要先转移所有权。
    /// </remarks>
    Task<IReadOnlySet<Guid>> OwnersAmongAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
