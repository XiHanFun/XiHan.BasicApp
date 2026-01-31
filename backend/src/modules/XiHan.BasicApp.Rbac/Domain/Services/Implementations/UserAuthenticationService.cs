#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAuthenticationService
// Guid:b4c5d6e7-f8a9-bcde-f123-4567890abcde
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Domain.Services.Implementations;

/// <summary>
/// 用户认证领域服务实现
/// </summary>
public class UserAuthenticationService : DomainService, IUserAuthenticationService
{
    private readonly ISysUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserAuthenticationService(ISysUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 验证用户凭证
    /// </summary>
    public async Task<SysUser?> AuthenticateAsync(string userName, string password, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(userName, tenantId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // 检查用户状态
        if (!IsUserActive(user))
        {
            return null;
        }

        // 验证密码
        if (!VerifyPassword(user, password))
        {
            return null;
        }

        // 检查租户归属
        if (tenantId.HasValue && !BelongsToTenant(user, tenantId.Value))
        {
            return null;
        }

        return user;
    }

    /// <summary>
    /// 验证用户密码
    /// </summary>
    public bool VerifyPassword(SysUser user, string password)
    {
        if (user == null || string.IsNullOrEmpty(user.Password))
        {
            return false;
        }

        return _passwordHasher.VerifyPassword(user.Password, password);
    }

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(password);
    }

    /// <summary>
    /// 检查用户是否被锁定
    /// </summary>
    public bool IsUserLocked(SysUser user)
    {
        // TODO: 实现用户锁定逻辑（如：登录失败次数过多、管理员锁定等）
        // 这里需要根据实际业务需求实现
        return false;
    }

    /// <summary>
    /// 检查用户是否处于活跃状态
    /// </summary>
    public bool IsUserActive(SysUser user)
    {
        if (user == null)
        {
            return false;
        }

        // 检查用户状态
        if (user.Status != YesOrNo.Yes)
        {
            return false;
        }

        // 检查是否被锁定
        if (IsUserLocked(user))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证用户是否属于指定租户
    /// </summary>
    public bool BelongsToTenant(SysUser user, long tenantId)
    {
        if (user == null)
        {
            return false;
        }

        return user.TenantId == tenantId;
    }
}
