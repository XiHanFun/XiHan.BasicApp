#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISessionRoleRepository
// Guid:c0285317-2aca-43ad-96dc-0c85013b79be
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
