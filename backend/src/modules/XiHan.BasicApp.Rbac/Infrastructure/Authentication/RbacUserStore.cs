#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacUserStore
// Guid:1532ef22-2832-4537-97cf-1615fd6fd76c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 17:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Authentication;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Authentication;

/// <summary>
/// 基于 RBAC 数据库的用户存储实现
/// </summary>
public class RbacUserStore : IUserStore
{
    private const string RecoveryCodeConfigGroup = "Framework.Authentication";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IUserRepository _userRepository;
    private readonly IConfigRepository _configRepository;
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="configRepository">配置仓储</param>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="currentTenant">当前租户</param>
    public RbacUserStore(
        IUserRepository userRepository,
        IConfigRepository configRepository,
        ISqlSugarDbContext dbContext,
        ICurrentTenant currentTenant)
    {
        _userRepository = userRepository;
        _configRepository = configRepository;
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    public async Task<UserInfo?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var tenantId = _currentTenant.Id;
        var user = await _userRepository.GetByUserNameAsync(username.Trim(), tenantId, cancellationToken);
        return user is null ? null : await MapToUserInfoAsync(user, cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    public async Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!TryParseUserId(userId, out var parsedUserId))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(parsedUserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (!IsTenantMatched(user.TenantId))
        {
            return null;
        }

        return await MapToUserInfoAsync(user, cancellationToken);
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateUserAsync(UserInfo user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!TryParseUserId(user.UserId, out var parsedUserId))
        {
            throw new ArgumentException("用户ID格式无效", nameof(user));
        }

        var existingUser = await _userRepository.GetByIdAsync(parsedUserId, cancellationToken)
            ?? throw new InvalidOperationException($"用户 {user.UserId} 不存在");

        if (!IsTenantMatched(existingUser.TenantId))
        {
            throw new InvalidOperationException($"用户 {user.UserId} 不在当前租户上下文中");
        }

        existingUser.UserName = string.IsNullOrWhiteSpace(user.Username) ? existingUser.UserName : user.Username.Trim();
        existingUser.Password = string.IsNullOrWhiteSpace(user.PasswordHash) ? existingUser.Password : user.PasswordHash;
        existingUser.Email = NormalizeNullable(user.Email);
        existingUser.Phone = NormalizeNullable(user.PhoneNumber);
        existingUser.Status = user.IsActive ? YesOrNo.Yes : YesOrNo.No;
        existingUser.LastLoginTime = ToDateTimeOffset(user.LastLoginTime);

        await DbClient.Updateable(existingUser).ExecuteCommandAsync(cancellationToken);

        var security = await EnsureSecurityAsync(existingUser.BasicId, existingUser.TenantId, cancellationToken);
        security.FailedLoginAttempts = Math.Max(0, user.FailedLoginAttempts);
        security.TwoFactorEnabled = user.TwoFactorEnabled;
        security.TwoFactorSecret = NormalizeNullable(user.TwoFactorSecret);
        security.LockoutEndTime = ToDateTimeOffset(user.LockoutEnd);
        security.IsLocked = user.IsLocked || (security.LockoutEndTime.HasValue && security.LockoutEndTime.Value > DateTimeOffset.UtcNow);
        security.LastPasswordChangeTime = ToDateTimeOffset(user.PasswordChangedTime);
        await _userRepository.SaveSecurityAsync(security, cancellationToken);

        await SaveRecoveryCodesAsync(existingUser.BasicId, existingUser.TenantId, user.RecoveryCodes, cancellationToken);
    }

    /// <summary>
    /// 更新用户密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="passwordHash">密码哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdatePasswordAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (!TryParseUserId(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("用户ID或密码哈希无效");
        }

        var user = await _userRepository.GetByIdAsync(parsedUserId, cancellationToken)
            ?? throw new InvalidOperationException($"用户 {userId} 不存在");

        if (!IsTenantMatched(user.TenantId))
        {
            throw new InvalidOperationException($"用户 {userId} 不在当前租户上下文中");
        }

        user.Password = passwordHash;
        await DbClient.Updateable(user).ExecuteCommandAsync(cancellationToken);

        var security = await EnsureSecurityAsync(user.BasicId, user.TenantId, cancellationToken);
        security.LastPasswordChangeTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 获取登录失败次数
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败次数</returns>
    public async Task<int> GetFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return 0;
        }

        var user = await _userRepository.GetByUserNameAsync(username.Trim(), _currentTenant.Id, cancellationToken);
        if (user is null)
        {
            return 0;
        }

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        return security?.FailedLoginAttempts ?? 0;
    }

    /// <summary>
    /// 记录登录失败
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task IncrementFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var user = await _userRepository.GetByUserNameAsync(username.Trim(), _currentTenant.Id, cancellationToken);
        if (user is null)
        {
            return;
        }

        var security = await EnsureSecurityAsync(user.BasicId, user.TenantId, cancellationToken);
        security.FailedLoginAttempts = Math.Max(0, security.FailedLoginAttempts) + 1;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 重置登录失败次数
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ResetFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var user = await _userRepository.GetByUserNameAsync(username.Trim(), _currentTenant.Id, cancellationToken);
        if (user is null)
        {
            return;
        }

        var security = await EnsureSecurityAsync(user.BasicId, user.TenantId, cancellationToken);
        security.FailedLoginAttempts = 0;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;
        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 设置账户锁定时间
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="lockoutEnd">锁定结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SetLockoutEndAsync(string username, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var user = await _userRepository.GetByUserNameAsync(username.Trim(), _currentTenant.Id, cancellationToken);
        if (user is null)
        {
            return;
        }

        var security = await EnsureSecurityAsync(user.BasicId, user.TenantId, cancellationToken);
        security.LockoutEndTime = ToDateTimeOffset(lockoutEnd);
        security.IsLocked = security.LockoutEndTime.HasValue && security.LockoutEndTime.Value > DateTimeOffset.UtcNow;
        security.LockoutTime = security.IsLocked ? DateTimeOffset.UtcNow : null;
        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 获取账户锁定结束时间
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁定结束时间</returns>
    public async Task<DateTime?> GetLockoutEndAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var user = await _userRepository.GetByUserNameAsync(username.Trim(), _currentTenant.Id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        return security?.LockoutEndTime?.UtcDateTime;
    }

    private async Task<UserInfo> MapToUserInfoAsync(SysUser user, CancellationToken cancellationToken)
    {
        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        var recoveryCodes = await GetRecoveryCodesAsync(user.BasicId, user.TenantId, cancellationToken);

        return new UserInfo
        {
            UserId = user.BasicId.ToString(CultureInfo.InvariantCulture),
            Username = user.UserName,
            PasswordHash = user.Password,
            Email = user.Email,
            PhoneNumber = user.Phone,
            TwoFactorEnabled = security?.TwoFactorEnabled ?? false,
            TwoFactorSecret = security?.TwoFactorSecret,
            RecoveryCodes = recoveryCodes,
            IsLocked = security?.IsLocked ?? false,
            LockoutEnd = security?.LockoutEndTime?.UtcDateTime,
            FailedLoginAttempts = security?.FailedLoginAttempts ?? 0,
            LastLoginTime = user.LastLoginTime?.UtcDateTime,
            PasswordChangedTime = security?.LastPasswordChangeTime?.UtcDateTime,
            IsActive = user.Status == YesOrNo.Yes
        };
    }

    private async Task<SysUserSecurity> EnsureSecurityAsync(long userId, long? tenantId, CancellationToken cancellationToken)
    {
        var security = await _userRepository.GetSecurityByUserIdAsync(userId, tenantId, cancellationToken);
        if (security is not null)
        {
            return security;
        }

        security = new SysUserSecurity
        {
            TenantId = tenantId,
            UserId = userId
        };

        return await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    private async Task<List<string>> GetRecoveryCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken)
    {
        var configKey = BuildRecoveryCodeConfigKey(userId, tenantId);
        var config = await _configRepository.GetByConfigKeyAsync(configKey, tenantId, cancellationToken);
        if (config is null || string.IsNullOrWhiteSpace(config.ConfigValue))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(config.ConfigValue, JsonSerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task SaveRecoveryCodesAsync(
        long userId,
        long? tenantId,
        IReadOnlyCollection<string>? recoveryCodes,
        CancellationToken cancellationToken)
    {
        var configKey = BuildRecoveryCodeConfigKey(userId, tenantId);
        var existing = await _configRepository.GetByConfigKeyAsync(configKey, tenantId, cancellationToken);

        var normalizedCodes = recoveryCodes?
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Select(static code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        if (normalizedCodes.Length == 0)
        {
            if (existing is not null)
            {
                await DbClient.Deleteable<SysConfig>()
                    .Where(config => config.BasicId == existing.BasicId)
                    .ExecuteCommandAsync(cancellationToken);
            }

            return;
        }

        var payload = JsonSerializer.Serialize(normalizedCodes, JsonSerializerOptions);
        if (existing is null)
        {
            var config = new SysConfig
            {
                TenantId = tenantId,
                ConfigName = $"RecoveryCodes:{userId}",
                ConfigGroup = RecoveryCodeConfigGroup,
                ConfigKey = configKey,
                ConfigValue = payload,
                ConfigType = ConfigType.User,
                DataType = ConfigDataType.Array,
                ConfigDescription = $"User recovery codes: {userId}",
                IsBuiltIn = false,
                IsEncrypted = true,
                Status = YesOrNo.Yes
            };

            await DbClient.Insertable(config).ExecuteCommandAsync(cancellationToken);
            return;
        }

        existing.ConfigValue = payload;
        existing.Status = YesOrNo.Yes;
        await DbClient.Updateable(existing).ExecuteCommandAsync(cancellationToken);
    }

    private static string BuildRecoveryCodeConfigKey(long userId, long? tenantId)
    {
        var tenantPart = tenantId.HasValue ? tenantId.Value.ToString(CultureInfo.InvariantCulture) : "host";
        var raw = $"auth-recovery|{tenantPart}|{userId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"auth:{hash}";
    }

    private bool IsTenantMatched(long? tenantId)
    {
        var currentTenantId = _currentTenant.Id;
        return currentTenantId.HasValue ? tenantId == currentTenantId : tenantId is null;
    }

    private static bool TryParseUserId(string? userId, out long parsedUserId)
    {
        return long.TryParse(userId, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedUserId);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var utc = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }
}
