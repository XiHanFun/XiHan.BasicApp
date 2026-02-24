#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserAuthenticationService
// Guid:b4c5d6e7-f8a9-bcde-f123-4567890abcde
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.DomainServices;

/// <summary>
/// 用户认证领域服务接口
/// </summary>
public interface IUserAuthenticationService : IDomainService
{
    /// <summary>
    /// 验证用户凭证
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">密码（明文）</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证结果（成功返回用户实体，失败返回 null）</returns>
    Task<SysUser?> AuthenticateAsync(string userName, string password, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户密码
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="password">密码（明文）</param>
    /// <returns>是否验证成功</returns>
    bool VerifyPassword(SysUser user, string password);

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    /// <param name="password">密码（明文）</param>
    /// <returns>密码哈希</returns>
    string HashPassword(string password);

    /// <summary>
    /// 检查用户是否被锁定
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>是否被锁定</returns>
    bool IsUserLocked(SysUser user);

    /// <summary>
    /// 检查用户是否处于活跃状态
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>是否活跃</returns>
    bool IsUserActive(SysUser user);

    /// <summary>
    /// 验证用户是否属于指定租户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否属于该租户</returns>
    bool BelongsToTenant(SysUser user, long tenantId);

    /// <summary>
    /// 记录登录失败并检查是否需要锁定账户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已锁定账户</returns>
    Task<bool> RecordFailedLoginAttemptAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置登录失败次数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ResetFailedLoginAttemptsAsync(long userId, CancellationToken cancellationToken = default);
}
