#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleHierarchyRepository
// Guid:61cbb96e-fca3-41ff-9f3e-a42f5fcb2d98
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色层级关系仓储接口
/// </summary>
public interface IRoleHierarchyRepository
{
    /// <summary>
    /// 获取角色集合的有效角色ID（包含继承祖先角色）
    /// </summary>
    Task<IReadOnlyCollection<long>> GetInheritedRoleIdsAsync(
        IReadOnlyCollection<long> roleIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的直接父角色ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetDirectParentRoleIdsAsync(
        long roleId,
        long? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换角色直接父角色（自动重建闭包关系）
    /// </summary>
    Task ReplaceDirectParentsAsync(
        long roleId,
        IReadOnlyCollection<long> parentRoleIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建角色层级闭包表
    /// </summary>
    Task RebuildHierarchyAsync(long? tenantId = null, CancellationToken cancellationToken = default);
}
