#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSessionRepository
// Guid:38a19434-31ff-4002-a79f-ce1d26fe6f85
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户会话仓储接口
/// </summary>
public interface IUserSessionRepository : IAggregateRootRepository<SysUserSession, long>
{
    /// <summary>
    /// 根据会话ID获取会话
    /// </summary>
    Task<SysUserSession?> GetBySessionIdAsync(string sessionId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户的在线会话
    /// </summary>
    Task<int> RevokeUserSessionsAsync(long userId, string reason, long? tenantId = null, CancellationToken cancellationToken = default);
}
