// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户会话领域服务
/// </summary>
/// <remarks>
/// 职责：会话并发控制策略、会话有效性判断
/// </remarks>
public interface IUserSessionDomainService
{
    /// <summary>
    /// 判断会话是否有效
    /// </summary>
    /// <param name="session">会话实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>是否有效</returns>
    bool IsSessionValid(SysUserSession session, DateTimeOffset now);

    /// <summary>
    /// 筛选需要被挤下线的会话（基于并发策略）
    /// </summary>
    /// <param name="activeSessions">用户当前活跃会话列表</param>
    /// <param name="maxConcurrent">最大并发会话数</param>
    /// <returns>需要被吊销的会话列表</returns>
    IReadOnlyList<SysUserSession> GetSessionsToRevoke(IReadOnlyList<SysUserSession> activeSessions, int maxConcurrent);
}
