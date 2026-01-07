#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionCommandService
// Guid:e1f2a3b4-c5d6-4e7f-8a9b-0c1d2e3f4a5b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 用户会话命令服务（处理用户会话的写操作）
/// </summary>
public class UserSessionCommandService : CrudApplicationServiceBase<SysUserSession, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionCommandService(IUserSessionRepository userSessionRepository)
        : base(userSessionRepository)
    {
        _userSessionRepository = userSessionRepository;
    }

    /// <summary>
    /// 创建用户会话
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        var session = input.Adapt<SysUserSession>();

        // 生成会话Token
        session.SessionToken = GenerateSessionToken();
        session.LoginTime = DateTimeOffset.UtcNow;
        session.LastAccessTime = DateTimeOffset.UtcNow;
        session.IsActive = true;

        // 设置过期时间（例如：7天后过期）
        session.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);

        session = await _userSessionRepository.AddAsync(session);

        return await MapToEntityDtoAsync(session);
    }

    /// <summary>
    /// 更新会话最后访问时间
    /// </summary>
    /// <param name="sessionToken">会话Token</param>
    public async Task<bool> UpdateLastAccessTimeAsync(string sessionToken)
    {
        return await _userSessionRepository.UpdateLastAccessTimeAsync(sessionToken);
    }

    /// <summary>
    /// 注销会话
    /// </summary>
    /// <param name="sessionToken">会话Token</param>
    public async Task<bool> LogoutAsync(string sessionToken)
    {
        return await _userSessionRepository.LogoutAsync(sessionToken);
    }

    /// <summary>
    /// 批量注销用户的所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task<int> LogoutUserSessionsAsync(long userId)
    {
        return await _userSessionRepository.LogoutUserSessionsAsync(userId);
    }

    /// <summary>
    /// 清理过期会话
    /// </summary>
    public async Task<int> CleanupExpiredSessionsAsync()
    {
        return await _userSessionRepository.CleanupExpiredSessionsAsync();
    }

    /// <summary>
    /// 强制注销指定设备的会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceId">设备ID</param>
    public async Task<bool> ForceLogoutDeviceAsync(long userId, string deviceId)
    {
        var sessions = await _userSessionRepository.GetActiveSessionsByUserIdAsync(userId);
        var deviceSession = sessions.FirstOrDefault(s => s.DeviceId == deviceId);

        if (deviceSession != null)
        {
            return await _userSessionRepository.LogoutAsync(deviceSession.SessionToken);
        }

        return false;
    }

    /// <summary>
    /// 生成会话Token
    /// </summary>
    private static string GenerateSessionToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray() + Guid.NewGuid().ToByteArray());
    }
}
