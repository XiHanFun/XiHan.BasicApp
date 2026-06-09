#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleHierarchyRepository
// Guid:4728c85e-70d1-4f18-a4bd-fdeba7d80cbb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色层级仓储接口
/// </summary>
public interface IRoleHierarchyRepository : ISaasRepository<SysRoleHierarchy>
{
    /// <summary>
    /// 获取角色继承链中的祖先角色ID
    /// </summary>
    Task<IReadOnlyList<long>> GetAncestorIdsAsync(IEnumerable<long> roleIds, bool includeSelf, CancellationToken cancellationToken = default);
}
