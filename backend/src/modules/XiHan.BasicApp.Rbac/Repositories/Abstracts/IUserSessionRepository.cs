#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSessionRepository
// Guid:e3f4a5b6-c7d8-4e5f-9a0b-2c3d4e5f6a7b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 用户会话仓储接口
/// </summary>
public interface IUserSessionRepository : IAggregateRootRepository<SysUserSession, long>
{
    /// <summary>
    /// 根据会话令牌获取会话
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话实体</returns>
    Task<SysUserSession?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID获取所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话列表</returns>
    Task<List<SysUserSession>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的有效会话列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话列表</returns>
    Task<List<SysUserSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证会话令牌
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话实体（如果有效）</returns>
    Task<SysUserSession?> ValidateSessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新会话最后活动时间
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="lastActivityTime">最后活动时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateLastActivityTimeAsync(string sessionToken, DateTimeOffset lastActivityTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销会话
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户的所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="excludeSessionId">排除的会话ID（可选，用于保留当前会话）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销数量</returns>
    Task<int> RevokeUserSessionsAsync(long userId, long? excludeSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期会话
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanExpiredSessionsAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取在线用户数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>在线用户数量</returns>
    Task<int> GetOnlineUserCountAsync(CancellationToken cancellationToken = default);
}
