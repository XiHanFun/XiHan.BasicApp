#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyApplicationMapper
// Guid:e77a2e2e-a125-479b-abb0-44cf11f8ce7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色继承应用层映射器
/// </summary>
public static class RoleHierarchyApplicationMapper
{
    /// <summary>
    /// 映射角色继承列表项
    /// </summary>
    /// <param name="hierarchy">角色继承关系</param>
    /// <param name="ancestor">祖先角色</param>
    /// <param name="descendant">后代角色</param>
    /// <returns>角色继承列表项 DTO</returns>
    public static RoleHierarchyListItemDto ToListItemDto(SysRoleHierarchy hierarchy, SysRole? ancestor, SysRole? descendant)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);

        return new RoleHierarchyListItemDto
        {
            BasicId = hierarchy.BasicId,
            AncestorId = hierarchy.AncestorId,
            AncestorRoleCode = ancestor?.RoleCode,
            AncestorRoleName = ancestor?.RoleName,
            AncestorRoleType = ancestor?.RoleType,
            AncestorStatus = ancestor?.Status,
            IsAncestorGlobal = ancestor?.IsGlobal,
            DescendantId = hierarchy.DescendantId,
            DescendantRoleCode = descendant?.RoleCode,
            DescendantRoleName = descendant?.RoleName,
            DescendantRoleType = descendant?.RoleType,
            DescendantStatus = descendant?.Status,
            IsDescendantGlobal = descendant?.IsGlobal,
            Depth = hierarchy.Depth,
            Path = hierarchy.Path,
            Remark = hierarchy.Remark,
            CreatedTime = hierarchy.CreatedTime
        };
    }

    /// <summary>
    /// 映射角色继承详情
    /// </summary>
    /// <param name="hierarchy">角色继承关系</param>
    /// <param name="ancestor">祖先角色</param>
    /// <param name="descendant">后代角色</param>
    /// <returns>角色继承详情 DTO</returns>
    public static RoleHierarchyDetailDto ToDetailDto(SysRoleHierarchy hierarchy, SysRole? ancestor, SysRole? descendant)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);

        return new RoleHierarchyDetailDto
        {
            BasicId = hierarchy.BasicId,
            AncestorId = hierarchy.AncestorId,
            AncestorRoleCode = ancestor?.RoleCode,
            AncestorRoleName = ancestor?.RoleName,
            AncestorRoleType = ancestor?.RoleType,
            AncestorStatus = ancestor?.Status,
            IsAncestorGlobal = ancestor?.IsGlobal,
            DescendantId = hierarchy.DescendantId,
            DescendantRoleCode = descendant?.RoleCode,
            DescendantRoleName = descendant?.RoleName,
            DescendantRoleType = descendant?.RoleType,
            DescendantStatus = descendant?.Status,
            IsDescendantGlobal = descendant?.IsGlobal,
            Depth = hierarchy.Depth,
            Path = hierarchy.Path,
            Remark = hierarchy.Remark,
            CreatedTime = hierarchy.CreatedTime,
            CreatedId = hierarchy.CreatedId,
            CreatedBy = hierarchy.CreatedBy
        };
    }
}
