#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileQueryService
// Guid:a4f8a4c6-4ef8-4567-ad7c-37e0bb4d4080
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 当前用户个人中心查询服务实现
/// </summary>
public sealed class ProfileQueryService
    : IProfileQueryService
{
    private const int UserNameChangeIntervalDays = 90;

    private readonly IExternalLoginRepository _externalLoginRepository;

    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileQueryService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserSessionRepository userSessionRepository,
        IExternalLoginRepository externalLoginRepository,
        ISqlSugarClientResolver clientResolver)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userSessionRepository = userSessionRepository;
        _externalLoginRepository = externalLoginRepository;
        _clientResolver = clientResolver;
    }

    /// <inheritdoc />
    public async Task<ProfileUserSecurityContext> GetSecurityContextAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");
        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("用户安全记录不存在。");

        return new ProfileUserSecurityContext(user, security);
    }

    /// <inheritdoc />
    public async Task<List<ProfileExternalLoginDto>> GetLinkedAccountsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var accounts = await _externalLoginRepository.GetListAsync(item => item.UserId == userId, cancellationToken);
        return [.. accounts
            .OrderBy(item => item.Provider)
            .Select(item => new ProfileExternalLoginDto
            {
                Provider = item.Provider,
                ProviderDisplayName = item.ProviderDisplayName,
                Email = item.Email,
                AvatarUrl = item.AvatarUrl,
                LastLoginTime = item.LastLoginTime
            })];
    }

    /// <inheritdoc />
    public async Task<ProfileLoginLogPageDto> GetLoginLogsAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _clientResolver.GetCurrentClient().Queryable<SysLoginLog>()
            .Where(log => log.UserId == userId)
            .SplitTable()
            .OrderBy(log => log.LoginTime, OrderByType.Desc);

        RefAsync<int> total = 0;
        var logs = await query.ToPageListAsync(page, pageSize, total, cancellationToken);
        return new ProfileLoginLogPageDto
        {
            Items = [.. logs.Select(log => new ProfileLoginLogItemDto
            {
                LoginTime = log.LoginTime,
                LoginIp = log.LoginIp,
                LoginLocation = log.LoginLocation,
                Browser = log.Browser,
                Os = log.Os,
                LoginResult = (int)log.LoginResult,
                Message = log.Message
            })],
            Total = total
        };
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> GetProfileAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        var context = await GetSecurityContextAsync(userId, cancellationToken);
        return ToProfileDto(context.User, context.Security, tenantId);
    }

    /// <inheritdoc />
    public async Task<List<ProfileSessionDto>> GetSessionsAsync(long userId, string? currentSessionId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var expireFallback = now.AddYears(100);
        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == userId &&
                       !session.IsRevoked &&
                       SqlFunc.IsNull(session.ExpiresAt, expireFallback) > now,
            cancellationToken);

        return [.. sessions
            .OrderByDescending(session => string.Equals(session.UserSessionId, currentSessionId, StringComparison.Ordinal))
            .ThenByDescending(session => session.LastActivityTime)
            .Select(session => new ProfileSessionDto
            {
                SessionId = session.UserSessionId,
                DeviceName = session.DeviceName,
                DeviceType = (int)session.DeviceType,
                Browser = session.Browser,
                OperatingSystem = session.OperatingSystem,
                IpAddress = session.IpAddress,
                Location = session.Location,
                LoginTime = session.LoginTime,
                LastActivityTime = session.LastActivityTime,
                IsCurrent = string.Equals(session.UserSessionId, currentSessionId, StringComparison.Ordinal)
            })];
    }

    private static bool CanChangeUserName(SysUserSecurity security, DateTimeOffset now)
    {
        return !security.LastUserNameChangeTime.HasValue ||
               security.LastUserNameChangeTime.Value.AddDays(UserNameChangeIntervalDays) <= now;
    }

    private static UserProfileDto ToProfileDto(SysUser user, SysUserSecurity security, long? tenantId)
    {
        return new UserProfileDto
        {
            UserId = user.BasicId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            Gender = (int)user.Gender,
            Birthday = user.Birthday,
            TimeZone = user.TimeZone,
            Language = user.Language,
            Country = user.Country,
            Remark = user.Remark,
            TenantId = tenantId,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
            IsSystemAccount = user.IsSystemAccount,
            TwoFactorEnabled = security.TwoFactorEnabled,
            TwoFactorMethod = (int)security.TwoFactorMethod,
            EmailVerified = security.EmailVerified,
            PhoneVerified = security.PhoneVerified,
            LastPasswordChangeTime = security.LastPasswordChangeTime,
            LastUserNameChangeTime = security.LastUserNameChangeTime,
            CanChangeUserName = !user.IsSystemAccount && CanChangeUserName(security, DateTimeOffset.UtcNow)
        };
    }
}
