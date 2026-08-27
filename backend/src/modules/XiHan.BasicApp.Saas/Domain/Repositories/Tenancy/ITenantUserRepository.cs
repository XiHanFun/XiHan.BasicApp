// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 租户成员仓储接口
/// </summary>
public interface ITenantUserRepository : ISaasRepository<SysTenantUser>
{
    /// <summary>
    /// 获取用户可进入的租户成员关系
    /// </summary>
    Task<IReadOnlyList<SysTenantUser>> GetActiveByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定租户成员关系
    /// </summary>
    Task<SysTenantUser?> GetMembershipAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户在指定租户下的成员关系（按租户精确匹配，不受当前租户上下文影响）
    /// </summary>
    /// <param name="tenantId">租户主键</param>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成员关系，不存在返回 null</returns>
    Task<SysTenantUser?> GetMembershipAsync(long tenantId, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计指定租户已占用的席位数
    /// </summary>
    /// <remarks>
    /// 口径与鉴权一致：InviteStatus=Accepted、Status=Valid 且当前时间落在生效期内。
    /// 排除 MemberType=PlatformAdmin——平台方切入租户代管属于运维行为，不消耗客户购买的席位。
    /// 按 TenantId 精确匹配而非依赖全局租户过滤器：后者放行 TenantId=0 的平台级成员，
    /// 会把平台账号计进任意租户的席位。
    /// </remarks>
    /// <param name="tenantIds">租户主键集合</param>
    /// <param name="now">当前时间，用于判定成员关系是否在生效期内</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户主键到已占用席位数的映射；无成员的租户不在结果中</returns>
    Task<IReadOnlyDictionary<long, long>> CountActiveMembersByTenantIdsAsync(IReadOnlyCollection<long> tenantIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
