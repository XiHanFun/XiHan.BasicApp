#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRepository
// Guid:a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : IAggregateRootRepository<SysUser, long>
{
    /// <summary>
    /// 根据用户名查询用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱查询用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<SysUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号查询用户
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<SysUser?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByEmailAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByPhoneAsync(string phone, long? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户及其角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体（包含角色）</returns>
    Task<SysUser?> GetWithRolesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户及其权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体（包含权限）</returns>
    Task<SysUser?> GetWithPermissionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的所有用户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<List<SysUser>> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginIp">登录IP</param>
    /// <param name="loginTime">登录时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateLastLoginAsync(long userId, string loginIp, DateTimeOffset loginTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门ID列表</returns>
    Task<List<long>> GetUserDepartmentIdsAsync(long userId, CancellationToken cancellationToken = default);
}
