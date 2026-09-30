// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 角色继承读取：继承链与继承来的权限，口径与授权快照一致
/// </summary>
/// <remarks>角色继承查询与角色详情聚合共用；调用方负责权限与超管隐藏校验。</remarks>
public interface IRoleInheritanceReader
{
    /// <summary>
    /// 角色的全部上级（不含自身）
    /// </summary>
    Task<List<RoleInheritanceItemDto>> GetAncestorsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色的全部下级（不含自身）
    /// </summary>
    Task<List<RoleInheritanceItemDto>> GetDescendantsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色从生效的上级继承来的权限绑定
    /// </summary>
    Task<List<RoleInheritedPermissionDto>> GetInheritedPermissionsAsync(long roleId, CancellationToken cancellationToken = default);
}
