// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// 租户成员关系的查询契约。
/// </summary>
/// <remarks>
/// 用户除了注册地租户，还能以成员身份进入别的租户（切换租户、在其中被模仿）。
/// 由 Tenancy 实现、Identity 在切换租户与模仿登录时消费；注册地不算成员关系，由调用方另判。没有任何实现时视为没有成员关系。
/// </remarks>
public interface ITenantMembershipResolver
{
    /// <summary>
    /// <paramref name="userId"/> 是否是 <paramref name="tenantId"/> 的成员。
    /// </summary>
    Task<bool> IsMemberAsync(string tenantId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="tenantId"/> 的全部成员用户 ID（不含注册地在该租户的用户）。
    /// </summary>
    Task<IReadOnlyList<Guid>> MembersOfAsync(string tenantId, CancellationToken cancellationToken);
}
