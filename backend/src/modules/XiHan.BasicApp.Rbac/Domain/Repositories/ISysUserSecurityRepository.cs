#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserSecurityRepository
// Guid:876ee67b-5379-46e1-89f2-75c1344a1fa5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统用户安全仓储接口
/// </summary>
public interface ISysUserSecurityRepository : IReadOnlyRepositoryBase<SysUserSecurity, long>
{
    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全信息</returns>
    Task<SysUserSecurity?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存用户安全信息
    /// </summary>
    /// <param name="userSecurity">用户安全实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的用户安全实体</returns>
    Task<SysUserSecurity> SaveAsync(SysUserSecurity userSecurity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task IncrementFailedLoginAttemptsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ResetFailedLoginAttemptsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 锁定用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="lockoutEndTime">锁定结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task LockUserAsync(long userId, DateTimeOffset lockoutEndTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解锁用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UnlockUserAsync(long userId, CancellationToken cancellationToken = default);
}
