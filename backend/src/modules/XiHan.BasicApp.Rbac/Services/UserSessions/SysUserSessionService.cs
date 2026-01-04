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

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.UserSessions;
using XiHan.BasicApp.Rbac.Services.UserSessions.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.UserSessions;

/// <summary>
/// 系统用户会话服务实现
/// </summary>
public class SysUserSessionService : CrudApplicationServiceBase<SysUserSession, UserSessionDto, long, CreateUserSessionDto, UpdateUserSessionDto>, ISysUserSessionService
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
        return session?.Adapt<UserSessionDto>();
    }

    /// <summary>
    /// 根据RefreshToken获取会话
    /// </summary>
    public async Task<UserSessionDto?> GetByRefreshTokenAsync(string refreshToken)
    {
        var session = await _userSessionRepository.GetByRefreshTokenAsync(refreshToken);
        return session?.Adapt<UserSessionDto>();
    }

    /// <summary>
    /// 根据SessionId获取会话
    /// </summary>
    public async Task<UserSessionDto?> GetBySessionIdAsync(string sessionId)
    {
        var session = await _userSessionRepository.GetBySessionIdAsync(sessionId);
        return session?.Adapt<UserSessionDto>();
    }

    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    public async Task<List<UserSessionDto>> GetByUserIdAsync(long userId)
    {
        var sessions = await _userSessionRepository.GetByUserIdAsync(userId);
        return sessions.Adapt<List<UserSessionDto>>();
    }

    /// <summary>
    /// 获取用户的在线会话
    /// </summary>
    public async Task<List<UserSessionDto>> GetOnlineSessionsByUserIdAsync(long userId)
    {
        var sessions = await _userSessionRepository.GetOnlineSessionsByUserIdAsync(userId);
        return sessions.Adapt<List<UserSessionDto>>();
    }

    /// <summary>
    /// 获取所有在线会话
    /// </summary>
    public async Task<List<UserSessionDto>> GetAllOnlineSessionsAsync()
    {
        var sessions = await _userSessionRepository.GetAllOnlineSessionsAsync();
        return sessions.Adapt<List<UserSessionDto>>();
    }

    /// <summary>
    /// 撤销用户的所有会话（踢人下线）
    /// </summary>
    public async Task<int> RevokeUserSessionsAsync(long userId, string? reason = null)
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
    public async Task<int> RevokeOtherSessionsAsync(long userId, string currentSessionId, string? reason = null)
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
    public async Task<int> GetSessionCountByDeviceTypeAsync(long userId, DeviceType deviceType)
    {
        return await _userSessionRepository.GetSessionCountByDeviceTypeAsync(userId, deviceType);
    }

    #endregion 业务特定方法
}
