// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色继承应用层映射器
/// </summary>
public static class RoleHierarchyApplicationMapper
{
    /// <summary>
    /// 映射角色继承创建命令
    /// </summary>
    public static RoleHierarchyCreateCommand ToCreateCommand(RoleHierarchyCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new RoleHierarchyCreateCommand(input.AncestorId, input.DescendantId, input.Remark);
    }

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
