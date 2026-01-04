#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserSessionRepository
// Guid:ae2b3c4d-5e6f-7890-abcd-ef12345678a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.UserSessions;

/// <summary>
/// 系统用户会话仓储接口
/// </summary>
public interface ISysUserSessionRepository : IRepositoryBase<SysUserSession, long>
{
    /// <summary>
    /// 根据Token获取会话
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns></returns>
    Task<SysUserSession?> GetByTokenAsync(string token);

    /// <summary>
    /// 根据RefreshToken获取会话
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns></returns>
    Task<SysUserSession?> GetByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 根据SessionId获取会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task<SysUserSession?> GetBySessionIdAsync(string sessionId);

    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysUserSession>> GetByUserIdAsync(long userId);

    /// <summary>
    /// 获取用户的在线会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysUserSession>> GetOnlineSessionsByUserIdAsync(long userId);

    /// <summary>
    /// 获取所有在线会话
    /// </summary>
    /// <returns></returns>
    Task<List<SysUserSession>> GetAllOnlineSessionsAsync();

    /// <summary>
    /// 撤销用户的所有会话（踢人下线）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns></returns>
    Task<int> RevokeUserSessionsAsync(long userId, string? reason = null);

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns></returns>
    Task<bool> RevokeSessionAsync(string sessionId, string? reason = null);

    /// <summary>
    /// 撤销除当前会话外的其他所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="currentSessionId">当前会话ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns></returns>
    Task<int> RevokeOtherSessionsAsync(long userId, string currentSessionId, string? reason = null);

    /// <summary>
    /// 更新会话活动时间
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task<bool> UpdateActivityTimeAsync(string sessionId);

    /// <summary>
    /// 删除过期的会话
    /// </summary>
    /// <returns></returns>
    Task<int> DeleteExpiredSessionsAsync();

    /// <summary>
    /// 根据设备类型获取用户会话数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceType">设备类型</param>
    /// <returns></returns>
    Task<int> GetSessionCountByDeviceTypeAsync(long userId, DeviceType deviceType);
}

