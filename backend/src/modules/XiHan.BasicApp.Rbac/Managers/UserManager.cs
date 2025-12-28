#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserManager
// Guid:7b2b3c4d-5e6f-7890-abcd-ef12345678ac
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 系统用户领域管理器
/// </summary>
public class UserManager : DomainService
{
    private readonly ISysUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    public UserManager(ISysUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 验证用户名是否唯一
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> IsUserNameUniqueAsync(string userName, XiHanBasicAppIdType? excludeId = null)
    {
        return !await _userRepository.ExistsByUserNameAsync(userName, excludeId);
    }

    /// <summary>
    /// 验证邮箱是否唯一
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> IsEmailUniqueAsync(string email, XiHanBasicAppIdType? excludeId = null)
    {
        return !await _userRepository.ExistsByEmailAsync(email, excludeId);
    }

    /// <summary>
    /// 验证手机号是否唯一
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> IsPhoneUniqueAsync(string phone, XiHanBasicAppIdType? excludeId = null)
    {
        return !await _userRepository.ExistsByPhoneAsync(phone, excludeId);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public bool VerifyPassword(SysUser user, string password)
    {
        // 这里应该使用密码加密算法验证
        // 示例：return PasswordHasher.Verify(password, user.Password);
        return user.Password == password;
    }

    /// <summary>
    /// 加密密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public string HashPassword(string password)
    {
        // 这里应该使用密码加密算法
        // 示例：return PasswordHasher.Hash(password);
        return password;
    }
}
