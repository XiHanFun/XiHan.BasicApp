// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户会话仓储接口
/// </summary>
public interface IUserSessionRepository : ISaasRepository<SysUserSession>
{
    /// <summary>
    /// 获取用户活跃会话列表
    /// </summary>
    Task<IReadOnlyList<SysUserSession>> GetActiveSessionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按会话业务标识查询会话（跨租户，标识全局唯一；用于请求期会话有效性校验，不依赖当前租户上下文）
    /// </summary>
    Task<SysUserSession?> GetByUserSessionIdAsync(string userSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户获取用户在指定设备上的活跃会话（会话行带发起登录时租户戳，同设备旧会话下线须忽略租户过滤）
    /// </summary>
    Task<IReadOnlyList<SysUserSession>> GetActiveByUserAndDeviceIgnoreTenantAsync(long userId, string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 吊销用户所有会话
    /// </summary>
    Task<int> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
