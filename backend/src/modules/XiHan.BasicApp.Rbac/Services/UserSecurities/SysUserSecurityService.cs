#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurityService
// Guid:ed2b3c4d-5e6f-7890-abcd-ef12345678a3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.UserSecurities;
using XiHan.BasicApp.Rbac.Services.UserSecurities.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.UserSecurities;

/// <summary>
/// 系统用户安全服务实现
/// </summary>
public class SysUserSecurityService : CrudApplicationServiceBase<SysUserSecurity, UserSecurityDto, long, CreateUserSecurityDto, UpdateUserSecurityDto>, ISysUserSecurityService
{
    private readonly ISysUserSecurityRepository _userSecurityRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserSecurityService(ISysUserSecurityRepository userSecurityRepository) : base(userSecurityRepository)
    {
        _userSecurityRepository = userSecurityRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    public async Task<UserSecurityDto?> GetByUserIdAsync(long userId)
    {
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(userId);
        return userSecurity?.Adapt<UserSecurityDto>();
    }

    /// <summary>
    /// 获取锁定的用户列表
    /// </summary>
    public async Task<List<UserSecurityDto>> GetLockedUsersAsync()
    {
        var userSecurities = await _userSecurityRepository.GetLockedUsersAsync();
        return userSecurities.Adapt<List<UserSecurityDto>>();
    }

    /// <summary>
    /// 获取密码过期的用户列表
    /// </summary>
    public async Task<List<UserSecurityDto>> GetPasswordExpiredUsersAsync()
    {
        var userSecurities = await _userSecurityRepository.GetPasswordExpiredUsersAsync();
        return userSecurities.Adapt<List<UserSecurityDto>>();
    }

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    public async Task<bool> IncrementFailedLoginAttemptsAsync(long userId)
    {
        return await _userSecurityRepository.IncrementFailedLoginAttemptsAsync(userId);
    }

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    public async Task<bool> ResetFailedLoginAttemptsAsync(long userId)
    {
        return await _userSecurityRepository.ResetFailedLoginAttemptsAsync(userId);
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    public async Task<bool> LockUserAsync(long userId, DateTimeOffset? lockoutEndTime = null)
    {
        return await _userSecurityRepository.LockUserAsync(userId, lockoutEndTime);
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    public async Task<bool> UnlockUserAsync(long userId)
    {
        return await _userSecurityRepository.UnlockUserAsync(userId);
    }

    /// <summary>
    /// 更新安全戳（强制重新登录）
    /// </summary>
    public async Task<bool> UpdateSecurityStampAsync(long userId)
    {
        return await _userSecurityRepository.UpdateSecurityStampAsync(userId);
    }

    #endregion 业务特定方法
}
