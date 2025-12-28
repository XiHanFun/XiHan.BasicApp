#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSessionRepository
// Guid:be2b3c4d-5e6f-7890-abcd-ef12345678a5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.UserSessions;

/// <summary>
/// 系统用户会话仓储实现
/// </summary>
public class SysUserSessionRepository : SqlSugarRepositoryBase<SysUserSession, XiHanBasicAppIdType>, ISysUserSessionRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysUserSessionRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据Token获取会话
    /// </summary>
    public async Task<SysUserSession?> GetByTokenAsync(string token)
    {
        return await GetFirstAsync(s => s.Token == token);
    }

    /// <summary>
    /// 根据RefreshToken获取会话
    /// </summary>
    public async Task<SysUserSession?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await GetFirstAsync(s => s.RefreshToken == refreshToken);
    }

    /// <summary>
    /// 根据SessionId获取会话
    /// </summary>
    public async Task<SysUserSession?> GetBySessionIdAsync(string sessionId)
    {
        return await GetFirstAsync(s => s.SessionId == sessionId);
    }

    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    public async Task<List<SysUserSession>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserSession>()
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户的在线会话
    /// </summary>
    public async Task<List<SysUserSession>> GetOnlineSessionsByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserSession>()
            .Where(s => s.UserId == userId && s.IsOnline && !s.IsRevoked)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有在线会话
    /// </summary>
    public async Task<List<SysUserSession>> GetAllOnlineSessionsAsync()
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserSession>()
            .Where(s => s.IsOnline && !s.IsRevoked)
            .ToListAsync();
    }

    /// <summary>
    /// 撤销用户的所有会话（踢人下线）
    /// </summary>
    public async Task<int> RevokeUserSessionsAsync(XiHanBasicAppIdType userId, string? reason = null)
    {
        var sessions = await GetOnlineSessionsByUserIdAsync(userId);
        var now = DateTimeOffset.Now;
        
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.IsOnline = false;
            session.RevokedAt = now;
            session.RevokedReason = reason ?? "管理员强制下线";
            session.LogoutTime = now;
        }

        if (sessions.Count == 0) return 0;
        return await _dbContext.GetClient().Updateable(sessions).ExecuteCommandAsync();
    }

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    public async Task<bool> RevokeSessionAsync(string sessionId, string? reason = null)
    {
        var session = await GetBySessionIdAsync(sessionId);
        if (session == null) return false;

        session.IsRevoked = true;
        session.IsOnline = false;
        session.RevokedAt = DateTimeOffset.Now;
        session.RevokedReason = reason ?? "会话已撤销";
        session.LogoutTime = DateTimeOffset.Now;
        
        await UpdateAsync(session);
        return true;
    }

    /// <summary>
    /// 撤销除当前会话外的其他所有会话
    /// </summary>
    public async Task<int> RevokeOtherSessionsAsync(XiHanBasicAppIdType userId, string currentSessionId, string? reason = null)
    {
        var sessions = await _dbContext.GetClient()
            .Queryable<SysUserSession>()
            .Where(s => s.UserId == userId && s.SessionId != currentSessionId && s.IsOnline && !s.IsRevoked)
            .ToListAsync();

        var now = DateTimeOffset.Now;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.IsOnline = false;
            session.RevokedAt = now;
            session.RevokedReason = reason ?? "其他设备登录";
            session.LogoutTime = now;
        }

        if (sessions.Count == 0) return 0;
        return await _dbContext.GetClient().Updateable(sessions).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新会话活动时间
    /// </summary>
    public async Task<bool> UpdateActivityTimeAsync(string sessionId)
    {
        var session = await GetBySessionIdAsync(sessionId);
        if (session == null) return false;

        session.LastActivityTime = DateTimeOffset.Now;
        await UpdateAsync(session);
        return true;
    }

    /// <summary>
    /// 删除过期的会话
    /// </summary>
    public async Task<int> DeleteExpiredSessionsAsync()
    {
        var now = DateTimeOffset.Now;
        return await _dbContext.GetClient()
            .Deleteable<SysUserSession>()
            .Where(s => (s.TokenExpiresAt < now && s.RefreshTokenExpiresAt < now) || s.IsRevoked)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据设备类型获取用户会话数量
    /// </summary>
    public async Task<int> GetSessionCountByDeviceTypeAsync(XiHanBasicAppIdType userId, DeviceType deviceType)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserSession>()
            .Where(s => s.UserId == userId && s.DeviceType == deviceType && s.IsOnline && !s.IsRevoked)
            .CountAsync();
    }
}

