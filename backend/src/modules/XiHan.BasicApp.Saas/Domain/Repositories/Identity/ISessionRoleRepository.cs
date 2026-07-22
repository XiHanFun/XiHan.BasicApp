// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 会话角色仓储接口
/// </summary>
public interface ISessionRoleRepository : ISaasRepository<SysSessionRole>
{
    /// <summary>
    /// 根据会话ID获取激活的角色列表
    /// </summary>
    Task<IReadOnlyList<SysSessionRole>> GetBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);
}
