#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleHierarchyDomainService
// Guid:a1b2c3d4-1111-2222-3333-444455556666
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色继承领域服务
/// </summary>
/// <remarks>
/// 职责：角色继承链展开、环路检测、继承闭包表维护
/// </remarks>
public interface IRoleHierarchyDomainService
{
    /// <summary>
    /// 检测角色继承是否会形成环路
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="childRoleId">子角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在环路</returns>
    Task<bool> WouldCreateCycleAsync(long parentRoleId, long childRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色完整继承链（含自身）的所有角色ID
    /// </summary>
    /// <param name="roleIds">起始角色ID集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>展开后的全部角色ID（含继承链）</returns>
    Task<IReadOnlyList<long>> ExpandRoleHierarchyAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default);
}
