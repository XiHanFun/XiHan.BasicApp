#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserSecurityRepository
// Guid:ad2b3c4d-5e6f-7890-abcd-ef123456789f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.UserSecurities;

/// <summary>
/// 系统用户安全仓储接口
/// </summary>
public interface ISysUserSecurityRepository : IRepositoryBase<SysUserSecurity, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<SysUserSecurity?> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取锁定的用户列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysUserSecurity>> GetLockedUsersAsync();

    /// <summary>
    /// 获取密码过期的用户列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysUserSecurity>> GetPasswordExpiredUsersAsync();

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> IncrementFailedLoginAttemptsAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> ResetFailedLoginAttemptsAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 锁定用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="lockoutEndTime">锁定结束时间</param>
    /// <returns></returns>
    Task<bool> LockUserAsync(XiHanBasicAppIdType userId, DateTimeOffset? lockoutEndTime = null);

    /// <summary>
    /// 解锁用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> UnlockUserAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 更新安全戳
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> UpdateSecurityStampAsync(XiHanBasicAppIdType userId);
}

