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

    // 热力图展示窗口：默认近一年（53 周 ≈ 371 天），与前端日历方格一致
    private const int ActivityTrendDays = 371;

    private readonly IExternalLoginRepository _externalLoginRepository;

    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly IUserSessionRepository _userSessionRepository;

    private readonly IUserStatisticsRepository _userStatisticsRepository;

    private readonly IUserNotificationPreferenceRepository _notificationPreferenceRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileQueryService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserSessionRepository userSessionRepository,
        IExternalLoginRepository externalLoginRepository,
        IUserStatisticsRepository userStatisticsRepository,
        IUserNotificationPreferenceRepository notificationPreferenceRepository,
        ISqlSugarClientResolver clientResolver)
    {
        _notificationPreferenceRepository = notificationPreferenceRepository;
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userSessionRepository = userSessionRepository;
        _externalLoginRepository = externalLoginRepository;
        _userStatisticsRepository = userStatisticsRepository;
        _clientResolver = clientResolver;
    }

    /// <summary>
    /// 偏好实体 → DTO
    /// </summary>
    public static ProfileNotificationPreferenceDto ToPreferenceDto(SysUserNotificationPreference preference)
    {
        ArgumentNullException.ThrowIfNull(preference);

        return new ProfileNotificationPreferenceDto
        {
            ChannelInApp = preference.ChannelInApp,
            ChannelEmail = preference.ChannelEmail,
            ChannelSms = preference.ChannelSms,
            ChannelPush = preference.ChannelPush,
            ChannelBot = preference.ChannelBot,
            TypeAnnouncement = preference.TypeAnnouncement,
            TypeTask = preference.TypeTask,
            TypeApproval = preference.TypeApproval,
            TypeSecurity = preference.TypeSecurity,
            TypeMarketing = preference.TypeMarketing
        };
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
    public async Task<ProfileActivityDto> GetActivityAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var client = _clientResolver.GetCurrentClient();
        var result = new ProfileActivityDto();

        // 周期摘要与最后行为时间取自预聚合的用户统计快照（由定时任务按周期写入；缺失则保持默认 0）
        var stats = await _userStatisticsRepository.GetListAsync(item => item.UserId == userId, cancellationToken);
        if (stats.Count > 0)
        {
            result.Today = ToPeriodDto(GetLatestSnapshot(stats, StatisticsPeriod.Today));
            result.ThisWeek = ToPeriodDto(GetLatestSnapshot(stats, StatisticsPeriod.ThisWeek));
            result.ThisMonth = ToPeriodDto(GetLatestSnapshot(stats, StatisticsPeriod.ThisMonth));

            result.LastLoginTime = stats.Where(item => item.LastLoginTime.HasValue).Max(item => item.LastLoginTime);
            result.LastAccessTime = stats.Where(item => item.LastAccessTime.HasValue).Max(item => item.LastAccessTime);
            result.LastOperationTime = stats.Where(item => item.LastOperationTime.HasValue).Max(item => item.LastOperationTime);
        }

        // 趋势热力图：直接按天聚合原始操作/访问日志（近一年窗口），不依赖统计快照定时任务，
        // 保证热力图反映真实活跃。仅投影日期列到实体类型（走实体映射，规避标量 DateTimeOffset 物化的转换异常）。
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-(ActivityTrendDays - 1));
        var since = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var operationRows = await client.Queryable<SysOperationLog>()
            .Where(log => log.UserId == userId && log.OperationTime >= since)
            .SplitTable()
            .Select(log => new SysOperationLog { OperationTime = log.OperationTime })
            .ToListAsync(cancellationToken);

        var accessRows = await client.Queryable<SysAccessLog>()
            .Where(log => log.UserId == userId && log.AccessTime >= since)
            .SplitTable()
            .Select(log => new SysAccessLog { AccessTime = log.AccessTime })
            .ToListAsync(cancellationToken);

        var operationByDate = operationRows
            .GroupBy(item => DateOnly.FromDateTime(item.OperationTime.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());
        var accessByDate = accessRows
            .GroupBy(item => DateOnly.FromDateTime(item.AccessTime.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());

        // 每日在线分钟取自当日 Today 周期快照（聚合任务按日 upsert，天然形成逐日历史）
        var onlineByDate = stats
            .Where(item => item.Period == StatisticsPeriod.Today && item.StatisticsDate >= startDate)
            .GroupBy(item => item.StatisticsDate)
            .ToDictionary(group => group.Key, group => group.Max(item => item.OnlineTime) / 60);

        // 仅返回有活跃记录的日期（稀疏），前端日历自行补齐空白格
        result.Trend = [.. operationByDate.Keys
            .Union(accessByDate.Keys)
            .Union(onlineByDate.Keys)
            .OrderBy(date => date)
            .Select(date => new ProfileActivityTrendPointDto
            {
                Date = date,
                OperationCount = operationByDate.GetValueOrDefault(date),
                AccessCount = accessByDate.GetValueOrDefault(date),
                OnlineMinutes = onlineByDate.GetValueOrDefault(date)
            })];

        // 统计快照缺失时，用原始日志兜底最后行为时间，避免概要全为空
        if (result.LastOperationTime is null && operationRows.Count > 0)
        {
            result.LastOperationTime = operationRows.Max(item => item.OperationTime);
        }
        if (result.LastAccessTime is null && accessRows.Count > 0)
        {
            result.LastAccessTime = accessRows.Max(item => item.AccessTime);
        }

        return result;
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
                LoginResult = log.LoginResult,
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
                       session.Status != SessionStatus.Revoked &&
                       SqlFunc.IsNull(session.ExpirationTime, expireFallback) > now,
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

    /// <inheritdoc />
    public async Task<ProfileNotificationPreferenceDto> GetNotificationPreferenceAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var preference = await _notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        // 无记录时返回默认偏好（不落库，写入时再惰性创建）
        return preference is null ? new ProfileNotificationPreferenceDto() : ToPreferenceDto(preference);
    }

    private static SysUserStatistics? GetLatestSnapshot(IEnumerable<SysUserStatistics> stats, StatisticsPeriod period)
    {
        return stats
            .Where(item => item.Period == period)
            .OrderByDescending(item => item.StatisticsDate)
            .FirstOrDefault();
    }

    private static ProfileActivityPeriodDto ToPeriodDto(SysUserStatistics? stats)
    {
        if (stats is null)
        {
            return new ProfileActivityPeriodDto();
        }

        return new ProfileActivityPeriodDto
        {
            LoginCount = stats.LoginCount,
            AccessCount = stats.AccessCount,
            OperationCount = stats.OperationCount,
            OnlineTime = stats.OnlineTime
        };
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
            CanChangeUserName = !user.IsSystemAccount && CanChangeUserName(security, DateTimeOffset.UtcNow),
            IsLocked = security.IsLocked,
            LockoutEndTime = security.LockoutEndTime,
            FailedLoginAttempts = security.FailedLoginAttempts,
            LastFailedLoginTime = security.LastFailedLoginTime
        };
    }
}
