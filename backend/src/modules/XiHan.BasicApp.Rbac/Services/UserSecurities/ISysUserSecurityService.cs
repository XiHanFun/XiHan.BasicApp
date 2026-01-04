#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserSecurityService
// Guid:dd2b3c4d-5e6f-7890-abcd-ef12345678a2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Services.UserSecurities.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.UserSecurities;

/// <summary>
/// 系统用户安全服务接口
/// </summary>
public interface ISysUserSecurityService : ICrudApplicationService<UserSecurityDto, long, CreateUserSecurityDto, UpdateUserSecurityDto>
{
    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<UserSecurityDto?> GetByUserIdAsync(long userId);

    /// <summary>
    /// 获取锁定的用户列表
    /// </summary>
    /// <returns></returns>
    Task<List<UserSecurityDto>> GetLockedUsersAsync();

    /// <summary>
    /// 获取密码过期的用户列表
    /// </summary>
    /// <returns></returns>
    Task<List<UserSecurityDto>> GetPasswordExpiredUsersAsync();

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> IncrementFailedLoginAttemptsAsync(long userId);

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> ResetFailedLoginAttemptsAsync(long userId);

    /// <summary>
    /// 锁定用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="lockoutEndTime">锁定结束时间</param>
    /// <returns></returns>
    Task<bool> LockUserAsync(long userId, DateTimeOffset? lockoutEndTime = null);

    /// <summary>
    /// 解锁用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> UnlockUserAsync(long userId);

    /// <summary>
    /// 更新安全戳（强制重新登录）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<bool> UpdateSecurityStampAsync(long userId);
}
