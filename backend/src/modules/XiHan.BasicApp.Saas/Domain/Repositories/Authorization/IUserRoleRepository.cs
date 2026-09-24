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
    /// 跨租户获取持有指定角色有效授权的用户主键
    /// </summary>
    /// <remarks>
    /// 授权行带所属租户的戳，「谁持有某个平台角色」是全局事实，必须跨租户查找。
    /// </remarks>
    /// <param name="roleIds">角色主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>去重后的用户主键</returns>
    Task<IReadOnlyList<long>> GetValidUserIdsByRoleIdsIgnoreTenantAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default);
}
