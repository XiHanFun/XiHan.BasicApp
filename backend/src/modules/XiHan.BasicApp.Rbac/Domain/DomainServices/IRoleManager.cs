#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleManager
// Guid:5595880b-4097-4468-950e-e02e44f1d524
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:39:39
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 角色领域管理器
/// </summary>
public interface IRoleManager
{
    /// <summary>
    /// 校验角色编码唯一性
    /// </summary>
    Task EnsureRoleCodeUniqueAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分配角色权限
    /// </summary>
    Task AssignPermissionsAsync(SysRole role, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    Task AssignMenusAsync(long roleId, IReadOnlyCollection<long> menuIds, long? tenantId = null, CancellationToken cancellationToken = default);
}
