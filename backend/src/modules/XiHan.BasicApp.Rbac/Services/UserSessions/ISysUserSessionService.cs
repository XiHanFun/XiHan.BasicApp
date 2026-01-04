#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserSessionService
// Guid:de2b3c4d-5e6f-7890-abcd-ef12345678a7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.UserSessions.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.UserSessions;

/// <summary>
/// 系统用户会话服务接口
/// </summary>
public interface ISysUserSessionService : ICrudApplicationService<UserSessionDto, XiHanBasicAppIdType, CreateUserSessionDto, UpdateUserSessionDto>
{
    /// <summary>
    /// 根据Token获取会话
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns></returns>
    Task<UserSessionDto?> GetByTokenAsync(string token);

    /// <summary>
    /// 根据RefreshToken获取会话
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns></returns>
    Task<UserSessionDto?> GetByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 根据SessionId获取会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task<UserSessionDto?> GetBySessionIdAsync(string sessionId);

    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<UserSessionDto>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取用户的在线会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<UserSessionDto>> GetOnlineSessionsByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取所有在线会话
    /// </summary>
    /// <returns></returns>
    Task<List<UserSessionDto>> GetAllOnlineSessionsAsync();

    /// <summary>
    /// 撤销用户的所有会话（踢人下线）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns></returns>
    Task<int> RevokeUserSessionsAsync(XiHanBasicAppIdType userId, string? reason = null);

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
    Task<int> RevokeOtherSessionsAsync(XiHanBasicAppIdType userId, string currentSessionId, string? reason = null);

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
    Task<int> GetSessionCountByDeviceTypeAsync(XiHanBasicAppIdType userId, DeviceType deviceType);
}
