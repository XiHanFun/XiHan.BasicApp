#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleDataScopeRepository
// Guid:f973280b-f075-46be-9a1d-1d292068bfc0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色数据范围仓储接口
/// </summary>
public interface IRoleDataScopeRepository : ISaasRepository<SysRoleDataScope>
{
    /// <summary>
    /// 获取角色有效自定义数据范围
    /// </summary>
    Task<IReadOnlyList<SysRoleDataScope>> GetValidByRoleIdsAsync(IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
