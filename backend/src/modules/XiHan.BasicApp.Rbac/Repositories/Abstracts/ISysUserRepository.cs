#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserRepository
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 用户聚合仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysUser + SysUserSecurity + SysUserStatistics
/// </remarks>
public interface ISysUserRepository : IAggregateRootRepository<SysUser, long>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<SysUser?> GetByEmailAsync(string email, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    Task<SysUser?> GetByPhoneAsync(string phone, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户及安全信息
    /// </summary>
    Task<SysUser?> GetWithSecurityAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户及统计信息
    /// </summary>
    Task<SysUser?> GetWithStatisticsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新最后登录信息
    /// </summary>
    Task UpdateLastLoginAsync(long userId, string? ip = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取用户
    /// </summary>
    Task<List<SysUser>> GetByIdsAsync(IEnumerable<long> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<bool> ExistsByUserNameAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
