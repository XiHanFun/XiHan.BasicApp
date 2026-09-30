// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户角色仓储接口
/// </summary>
public interface IUserRoleRepository : ISaasRepository<SysUserRole>
{
    /// <summary>
    /// 获取用户有效角色授权
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetValidByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色在当前上下文此刻生效的授权
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetValidByRoleIdAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色在当前上下文占用的成员名额：状态有效且未过期的授权（尚未生效的预约同样占名额）
    /// </summary>
    Task<int> CountOccupiedByRoleIdAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户获取持有指定角色有效授权的用户主键
    /// </summary>
    /// <remarks>
    /// 授权行带所属租户的戳，「谁持有某个平台角色」是全局事实，必须跨租户查找。
    /// </remarks>
    /// <param name="roleIds">角色主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>去重后的用户主键</returns>
    Task<IReadOnlyList<long>> GetValidUserIdsByRoleIdsIgnoreTenantAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default);
    /// <summary>
    /// 一组角色在当前上下文此刻生效的授权（读共享口径，调用方按作用域再筛）
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetValidByRoleIdsAsync(IReadOnlyCollection<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 一组用户在当前上下文此刻生效的授权（读共享口径，调用方按作用域再筛）
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetValidByUserIdsAsync(IReadOnlyCollection<long> userIds, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户获取此刻有成员持有指定角色的租户（不含平台）
    /// </summary>
    /// <remarks>全局角色在各租户里分配，平台调整它的继承后据此找出要复核的租户。</remarks>
    Task<IReadOnlyList<long>> GetValidTenantIdsByRoleIdsIgnoreTenantAsync(IReadOnlyCollection<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
