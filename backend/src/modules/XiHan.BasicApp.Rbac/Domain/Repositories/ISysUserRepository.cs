#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserRepository
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统用户仓储接口
/// </summary>
public interface ISysUserRepository : IAggregateRootRepository<SysUser, long>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<SysUser?> GetByEmailAsync(string email, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<SysUser?> GetByPhoneAsync(string phone, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsUserNameExistsAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的用户列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="isActive">是否启用（null 表示不过滤）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<List<SysUser>> GetUsersByTenantAsync(long tenantId, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存用户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的用户实体</returns>
    Task<SysUser> SaveAsync(SysUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginIp">登录IP</param>
    /// <param name="loginTime">登录时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateLastLoginInfoAsync(long userId, string loginIp, DateTimeOffset loginTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableUserAsync(long userId, CancellationToken cancellationToken = default);
}
