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

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 系统用户领域管理器
/// </summary>
/// <remarks>
/// 职责：用户相关的领域业务规则和验证逻辑
/// </remarks>
public class UserManager : DomainService
{
    private readonly ISysUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="passwordHasher">密码哈希服务</param>
    public UserManager(ISysUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 验证用户名是否唯一
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> IsUserNameUniqueAsync(string userName, long? excludeId = null)
    {
        return !await _userRepository.ExistsByUserNameAsync(userName, excludeId);
    }

    /// <summary>
    /// 验证邮箱是否唯一
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> IsEmailUniqueAsync(string email, long? excludeId = null)
    {
        return !await _userRepository.ExistsByEmailAsync(email, excludeId);
    }

    /// <summary>
    /// 验证手机号是否唯一
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> IsPhoneUniqueAsync(string phone, long? excludeId = null)
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
        return _passwordHasher.VerifyPassword(user.Password, password);
    }

    /// <summary>
    /// 加密密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(password);
    }

    /// <summary>
    /// 检查密码是否需要重新哈希
    /// </summary>
    /// <param name="user">用户</param>
    /// <returns></returns>
    public bool NeedsPasswordRehash(SysUser user)
    {
        return _passwordHasher.NeedsRehash(user.Password);
    }

    /// <summary>
    /// 验证用户状态是否有效
    /// </summary>
    /// <param name="user">用户</param>
    /// <returns></returns>
    public bool IsUserActive(SysUser user)
    {
        return user.Status == Enums.YesOrNo.Yes && !user.IsDeleted;
    }

    /// <summary>
    /// 检查用户是否可以删除
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<bool> CanDeleteAsync(long userId)
    {
        // 检查用户是否存在未完成的任务、订单等
        // 这里可以添加更多业务规则
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null && !user.IsDeleted;
    }
}
