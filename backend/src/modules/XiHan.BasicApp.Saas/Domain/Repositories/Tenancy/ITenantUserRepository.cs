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
    /// 按关键字检索租户成员，返回命中成员的用户主键
    /// </summary>
    /// <remarks>
    /// 同时匹配成员自己的显示名 / 邀请备注 / 备注，与账号的用户名 / 昵称 / 真实姓名 / 邮箱（忽略大小写）。
    /// 外部成员的账号属于别的租户，账号信息跨租户读取。
    /// </remarks>
    /// <param name="tenantId">租户主键</param>
    /// <param name="keyword">关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命中成员的用户主键</returns>
    Task<IReadOnlyList<long>> SearchMemberUserIdsAsync(long tenantId, string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计指定租户已占用的席位数
    /// </summary>
    /// <remarks>
    /// 口径与鉴权一致：InviteStatus=Accepted、Status=Valid 且当前时间落在生效期内。
    /// 排除 MemberType=PlatformAdmin——平台方切入租户代管属于运维行为，不消耗客户购买的席位。
    /// 一次统计多个租户，按 TenantId 精确匹配（显式跨租户读取），不依赖当前上下文。
    /// </remarks>
    /// <param name="tenantIds">租户主键集合</param>
    /// <param name="now">当前时间，用于判定成员关系是否在生效期内</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户主键到已占用席位数的映射；无成员的租户不在结果中</returns>
    Task<IReadOnlyDictionary<long, long>> CountActiveMembersByTenantIdsAsync(IReadOnlyCollection<long> tenantIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
