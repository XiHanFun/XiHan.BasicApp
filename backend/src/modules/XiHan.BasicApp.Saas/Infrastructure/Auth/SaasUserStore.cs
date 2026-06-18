#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasUserStore
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567801
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Auth;

/// <summary>
/// SaaS 用户存储实现，桥接框架 <see cref="IUserStore"/> 与领域实体 SysUser / SysUserSecurity
/// </summary>
public sealed class SaasUserStore : IUserStore
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasUserStore(ISqlSugarClientResolver clientResolver, ICurrentTenant currentTenant)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    }

    /// <inheritdoc />
    public async Task<UserInfo?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var user = await FindUserByLoginAsync(username.Trim(), cancellationToken);
        if (user is null)
        {
            return null;
        }

        var db = _clientResolver.GetCurrentClient();
        var security = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == user.BasicId && !s.IsDeleted)
            .FirstAsync(cancellationToken);

        return MapToUserInfo(user, security);
    }

    /// <inheritdoc />
    public async Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userId) || !long.TryParse(userId, out var id))
        {
            return null;
        }

        var db = _clientResolver.GetCurrentClient();

        var user = await db.Queryable<SysUser>()
            .Where(u => u.BasicId == id && !u.IsDeleted)
            .FirstAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var security = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == id && !s.IsDeleted)
            .FirstAsync(cancellationToken);

        return MapToUserInfo(user, security);
    }

    /// <inheritdoc />
    public async Task UpdateUserAsync(UserInfo user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user is null || string.IsNullOrWhiteSpace(user.UserId) || !long.TryParse(user.UserId, out var id))
        {
            throw new ArgumentException("用户信息或用户ID无效。", nameof(user));
        }

        var db = _clientResolver.GetCurrentClient();

        var sysUser = await db.Queryable<SysUser>()
            .Where(u => u.BasicId == id && !u.IsDeleted)
            .FirstAsync(cancellationToken)
            ?? throw new InvalidOperationException($"用户 {id} 不存在。");

        // 映射 UserInfo 可修改字段回 SysUser
        if (user.LastLoginTime.HasValue)
        {
            sysUser.LastLoginTime = new DateTimeOffset(user.LastLoginTime.Value, TimeSpan.Zero);
        }

        sysUser.Status = user.IsActive ? EnableStatus.Enabled : EnableStatus.Disabled;

        await db.Updateable(sysUser)
            .UpdateColumns(u => new { u.LastLoginTime, u.Status })
            .ExecuteCommandAsync(cancellationToken);

        // 映射 UserInfo 安全字段回 SysUserSecurity
        var security = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == id && !s.IsDeleted)
            .FirstAsync(cancellationToken);

        if (security is not null)
        {
            security.TwoFactorEnabled = user.TwoFactorEnabled;
            security.TwoFactorSecret = user.TwoFactorSecret;
            security.IsLocked = user.IsLocked;
            security.LockoutEndTime = user.LockoutEnd.HasValue
                ? new DateTimeOffset(user.LockoutEnd.Value, TimeSpan.Zero)
                : null;
            security.FailedLoginAttempts = user.FailedLoginAttempts;

            if (user.PasswordChangedTime.HasValue)
            {
                security.LastPasswordChangeTime = new DateTimeOffset(user.PasswordChangedTime.Value, TimeSpan.Zero);
            }

            await db.Updateable(security)
                .UpdateColumns(s => new
                {
                    s.TwoFactorEnabled,
                    s.TwoFactorSecret,
                    s.IsLocked,
                    s.LockoutEndTime,
                    s.FailedLoginAttempts,
                    s.LastPasswordChangeTime
                })
                .ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UpdatePasswordAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userId) || !long.TryParse(userId, out var id))
        {
            throw new ArgumentException("用户ID无效。", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("密码哈希不能为空。", nameof(passwordHash));
        }

        var db = _clientResolver.GetCurrentClient();

        var security = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == id && !s.IsDeleted)
            .FirstAsync(cancellationToken)
            ?? throw new InvalidOperationException($"用户 {id} 的安全记录不存在。");

        security.Password = passwordHash;
        security.LastPasswordChangeTime = DateTimeOffset.UtcNow;

        await db.Updateable(security)
            .UpdateColumns(s => new { s.Password, s.LastPasswordChangeTime })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return 0;
        }

        var userId = await FindUserIdByLoginAsync(username.Trim(), cancellationToken);
        if (userId == 0)
        {
            return 0;
        }

        var db = _clientResolver.GetCurrentClient();
        return await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => s.FailedLoginAttempts)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task IncrementFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var userId = await FindUserIdByLoginAsync(username.Trim(), cancellationToken);
        if (userId == 0)
        {
            return;
        }

        var db = _clientResolver.GetCurrentClient();
        var security = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .FirstAsync(cancellationToken);

        if (security is null)
        {
            return;
        }

        security.FailedLoginAttempts++;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;

        await db.Updateable(security)
            .UpdateColumns(s => new { s.FailedLoginAttempts, s.LastFailedLoginTime })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var userId = await FindUserIdByLoginAsync(username.Trim(), cancellationToken);
        if (userId == 0)
        {
            return;
        }

        var db = _clientResolver.GetCurrentClient();
        await db.Updateable<SysUserSecurity>()
            .SetColumns(s => s.FailedLoginAttempts == 0)
            .SetColumns(s => s.LastFailedLoginTime == null)
            .SetColumns(s => s.IsLocked == false)
            .SetColumns(s => s.LockoutTime == null)
            .SetColumns(s => s.LockoutEndTime == null)
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetLockoutEndAsync(string username, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var userId = await FindUserIdByLoginAsync(username.Trim(), cancellationToken);
        if (userId == 0)
        {
            return;
        }

        var db = _clientResolver.GetCurrentClient();
        var security = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .FirstAsync(cancellationToken);

        if (security is null)
        {
            return;
        }

        if (lockoutEnd.HasValue)
        {
            security.IsLocked = true;
            security.LockoutTime = DateTimeOffset.UtcNow;
            security.LockoutEndTime = new DateTimeOffset(lockoutEnd.Value, TimeSpan.Zero);
        }
        else
        {
            security.IsLocked = false;
            security.LockoutTime = null;
            security.LockoutEndTime = null;
        }

        await db.Updateable(security)
            .UpdateColumns(s => new { s.IsLocked, s.LockoutTime, s.LockoutEndTime })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLockoutEndAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var userId = await FindUserIdByLoginAsync(username.Trim(), cancellationToken);
        if (userId == 0)
        {
            return null;
        }

        var db = _clientResolver.GetCurrentClient();
        var lockoutEndTime = await db.Queryable<SysUserSecurity>()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => s.LockoutEndTime)
            .FirstAsync(cancellationToken);

        return lockoutEndTime?.UtcDateTime;
    }

    /// <summary>
    /// 将领域实体映射为框架 UserInfo
    /// </summary>
    private static UserInfo MapToUserInfo(SysUser user, SysUserSecurity? security)
    {
        return new UserInfo
        {
            UserId = user.BasicId.ToString(),
            Username = user.UserName,
            PasswordHash = security?.Password ?? string.Empty,
            Email = user.Email,
            PhoneNumber = user.Phone,
            TwoFactorEnabled = security?.TwoFactorEnabled ?? false,
            TwoFactorSecret = security?.TwoFactorSecret,
            RecoveryCodes = [],
            IsLocked = security?.IsLocked ?? false,
            LockoutEnd = security?.LockoutEndTime?.UtcDateTime,
            FailedLoginAttempts = security?.FailedLoginAttempts ?? 0,
            LastLoginTime = user.LastLoginTime?.UtcDateTime,
            PasswordChangedTime = security?.LastPasswordChangeTime?.UtcDateTime,
            IsActive = user.Status == EnableStatus.Enabled
        };
    }

    /// <summary>
    /// 按登录标识定位用户。
    /// </summary>
    /// <remarks>
    /// 登录身份模型（先登录后选租户）：
    /// - 无租户上下文（标准登录路径）：含 @ 视为邮箱，按全平台唯一邮箱定位（UX_Em）；
    ///   不含 @ 回退平台账号用户名定位（TenantId=0，如 superadmin），普通租户用户必须用邮箱登录。
    /// - 有租户上下文（租户内嵌登录等特殊场景）：沿用 租户内用户名 定位（UX_TeId_UsNa）。
    /// </remarks>
    private async Task<SysUser?> FindUserByLoginAsync(string login, CancellationToken cancellationToken)
    {
        var db = _clientResolver.GetCurrentClient();
        var tenantId = _currentTenant.Id;

        if (tenantId is null or 0)
        {
            return login.Contains('@')
                ? await db.Queryable<SysUser>()
                    .Where(u => u.Email == login && !u.IsDeleted)
                    .FirstAsync(cancellationToken)
                : await db.Queryable<SysUser>()
                    .Where(u => u.UserName == login && u.TenantId == 0 && !u.IsDeleted)
                    .FirstAsync(cancellationToken);
        }

        return await db.Queryable<SysUser>()
            .Where(u => u.UserName == login && u.TenantId == tenantId.Value && !u.IsDeleted)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 按登录标识定位用户主键，未找到返回 0。
    /// </summary>
    private async Task<long> FindUserIdByLoginAsync(string login, CancellationToken cancellationToken)
    {
        var user = await FindUserByLoginAsync(login, cancellationToken);
        return user?.BasicId ?? 0;
    }
}
