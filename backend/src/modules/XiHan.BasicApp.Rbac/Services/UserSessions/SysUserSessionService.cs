#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSessionService
// Guid:ee2b3c4d-5e6f-7890-abcd-ef12345678a8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.UserSessions;
using XiHan.BasicApp.Rbac.Services.UserSessions.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.UserSessions;

/// <summary>
/// 系统用户会话服务实现
/// </summary>
public class SysUserSessionService : CrudApplicationServiceBase<SysUserSession, UserSessionDto, XiHanBasicAppIdType, CreateUserSessionDto, UpdateUserSessionDto>, ISysUserSessionService
{
    private readonly ISysUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserSessionService(ISysUserSessionRepository userSessionRepository) : base(userSessionRepository)
    {
        _userSessionRepository = userSessionRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据Token获取会话
    /// </summary>
    public async Task<UserSessionDto?> GetByTokenAsync(string token)
    {
        var session = await _userSessionRepository.GetByTokenAsync(token);
        return session?.ToDto();
    }

    /// <summary>
    /// 根据RefreshToken获取会话
    /// </summary>
    public async Task<UserSessionDto?> GetByRefreshTokenAsync(string refreshToken)
    {
        var session = await _userSessionRepository.GetByRefreshTokenAsync(refreshToken);
        return session?.ToDto();
    }

    /// <summary>
    /// 根据SessionId获取会话
    /// </summary>
    public async Task<UserSessionDto?> GetBySessionIdAsync(string sessionId)
    {
        var session = await _userSessionRepository.GetBySessionIdAsync(sessionId);
        return session?.ToDto();
    }

    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    public async Task<List<UserSessionDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var sessions = await _userSessionRepository.GetByUserIdAsync(userId);
        return sessions.ToDto();
    }

    /// <summary>
    /// 获取用户的在线会话
    /// </summary>
    public async Task<List<UserSessionDto>> GetOnlineSessionsByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var sessions = await _userSessionRepository.GetOnlineSessionsByUserIdAsync(userId);
        return sessions.ToDto();
    }

    /// <summary>
    /// 获取所有在线会话
    /// </summary>
    public async Task<List<UserSessionDto>> GetAllOnlineSessionsAsync()
    {
        var sessions = await _userSessionRepository.GetAllOnlineSessionsAsync();
        return sessions.ToDto();
    }

    /// <summary>
    /// 撤销用户的所有会话（踢人下线）
    /// </summary>
    public async Task<int> RevokeUserSessionsAsync(XiHanBasicAppIdType userId, string? reason = null)
    {
        return await _userSessionRepository.RevokeUserSessionsAsync(userId, reason);
    }

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    public async Task<bool> RevokeSessionAsync(string sessionId, string? reason = null)
    {
        return await _userSessionRepository.RevokeSessionAsync(sessionId, reason);
    }

    /// <summary>
    /// 撤销除当前会话外的其他所有会话
    /// </summary>
    public async Task<int> RevokeOtherSessionsAsync(XiHanBasicAppIdType userId, string currentSessionId, string? reason = null)
    {
        return await _userSessionRepository.RevokeOtherSessionsAsync(userId, currentSessionId, reason);
    }

    /// <summary>
    /// 更新会话活动时间
    /// </summary>
    public async Task<bool> UpdateActivityTimeAsync(string sessionId)
    {
        return await _userSessionRepository.UpdateActivityTimeAsync(sessionId);
    }

    /// <summary>
    /// 删除过期的会话
    /// </summary>
    public async Task<int> DeleteExpiredSessionsAsync()
    {
        return await _userSessionRepository.DeleteExpiredSessionsAsync();
    }

    /// <summary>
    /// 根据设备类型获取用户会话数量
    /// </summary>
    public async Task<int> GetSessionCountByDeviceTypeAsync(XiHanBasicAppIdType userId, DeviceType deviceType)
    {
        return await _userSessionRepository.GetSessionCountByDeviceTypeAsync(userId, deviceType);
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<UserSessionDto> MapToEntityDtoAsync(SysUserSession entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 UserSessionDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysUserSession> MapToEntityAsync(UserSessionDto dto)
    {
        var entity = new SysUserSession
        {
            UserId = dto.UserId,
            Token = dto.Token,
            RefreshToken = dto.RefreshToken,
            SessionId = dto.SessionId,
            DeviceType = dto.DeviceType,
            DeviceName = dto.DeviceName,
            DeviceId = dto.DeviceId,
            OperatingSystem = dto.OperatingSystem,
            Browser = dto.Browser,
            IpAddress = dto.IpAddress,
            Location = dto.Location,
            LoginTime = dto.LoginTime,
            LastActivityTime = dto.LastActivityTime,
            TokenExpiresAt = dto.TokenExpiresAt,
            RefreshTokenExpiresAt = dto.RefreshTokenExpiresAt,
            IsOnline = dto.IsOnline,
            IsRevoked = dto.IsRevoked,
            RevokedAt = dto.RevokedAt,
            RevokedReason = dto.RevokedReason,
            LogoutTime = dto.LogoutTime,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 UserSessionDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(UserSessionDto dto, SysUserSession entity)
    {
        entity.UserId = dto.UserId;
        entity.Token = dto.Token;
        entity.RefreshToken = dto.RefreshToken;
        entity.SessionId = dto.SessionId;
        entity.DeviceType = dto.DeviceType;
        entity.DeviceName = dto.DeviceName;
        entity.DeviceId = dto.DeviceId;
        entity.OperatingSystem = dto.OperatingSystem;
        entity.Browser = dto.Browser;
        entity.IpAddress = dto.IpAddress;
        entity.Location = dto.Location;
        entity.LoginTime = dto.LoginTime;
        entity.LastActivityTime = dto.LastActivityTime;
        entity.TokenExpiresAt = dto.TokenExpiresAt;
        entity.RefreshTokenExpiresAt = dto.RefreshTokenExpiresAt;
        entity.IsOnline = dto.IsOnline;
        entity.IsRevoked = dto.IsRevoked;
        entity.RevokedAt = dto.RevokedAt;
        entity.RevokedReason = dto.RevokedReason;
        entity.LogoutTime = dto.LogoutTime;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysUserSession> MapToEntityAsync(CreateUserSessionDto createDto)
    {
        var entity = new SysUserSession
        {
            UserId = createDto.UserId,
            Token = createDto.Token,
            RefreshToken = createDto.RefreshToken,
            SessionId = Guid.NewGuid().ToString("N"),
            DeviceType = createDto.DeviceType,
            DeviceName = createDto.DeviceName,
            DeviceId = createDto.DeviceId,
            OperatingSystem = createDto.OperatingSystem,
            Browser = createDto.Browser,
            IpAddress = createDto.IpAddress,
            Location = createDto.Location,
            TokenExpiresAt = createDto.TokenExpiresAt,
            RefreshTokenExpiresAt = createDto.RefreshTokenExpiresAt,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateUserSessionDto updateDto, SysUserSession entity)
    {
        if (updateDto.LastActivityTime.HasValue) entity.LastActivityTime = updateDto.LastActivityTime.Value;
        if (updateDto.IsOnline.HasValue) entity.IsOnline = updateDto.IsOnline.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
