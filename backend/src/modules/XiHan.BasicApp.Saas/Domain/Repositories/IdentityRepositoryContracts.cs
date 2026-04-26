#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IdentityRepositoryContracts
// Guid:d0baac52-2b14-45e4-948f-e1d7564ae856
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : ISaasAggregateRepository<SysUser>
{
    /// <summary>
    /// 根据租户和用户名获取用户
    /// </summary>
    Task<SysUser?> GetByUserNameAsync(long tenantId, string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<bool> ExistsUserNameAsync(long tenantId, string userName, long? excludeUserId = null, CancellationToken cancellationToken = default);
}

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
    Task<SysTenantUser?> GetMembershipAsync(long tenantId, long userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户角色仓储接口
/// </summary>
public interface IUserRoleRepository : ISaasRepository<SysUserRole>
{
    /// <summary>
    /// 获取用户有效角色授权
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetValidByUserIdAsync(long tenantId, long userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户直授权限仓储接口
/// </summary>
public interface IUserPermissionRepository : ISaasRepository<SysUserPermission>
{
    /// <summary>
    /// 获取用户有效直授权限
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetValidByUserIdAsync(long tenantId, long userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
