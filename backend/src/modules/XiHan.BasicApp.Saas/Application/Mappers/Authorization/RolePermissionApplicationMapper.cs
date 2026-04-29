#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionApplicationMapper
// Guid:97e7ec19-581b-485f-8aa8-e846c367d441
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色权限应用层映射器
/// </summary>
public static class RolePermissionApplicationMapper
{
    /// <summary>
    /// 映射角色权限列表项
    /// </summary>
    /// <param name="rolePermission">角色权限绑定</param>
    /// <param name="permission">权限定义</param>
    /// <returns>角色权限列表项 DTO</returns>
    public static RolePermissionListItemDto ToListItemDto(SysRolePermission rolePermission, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(rolePermission);

        return new RolePermissionListItemDto
        {
            BasicId = rolePermission.BasicId,
            RoleId = rolePermission.RoleId,
            PermissionId = rolePermission.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            PermissionType = permission?.PermissionType,
            ModuleCode = permission?.ModuleCode,
            IsGlobalPermission = permission?.IsGlobal,
            IsRequireAudit = permission?.IsRequireAudit,
            PermissionStatus = permission?.Status,
            PermissionAction = rolePermission.PermissionAction,
            EffectiveTime = rolePermission.EffectiveTime,
            ExpirationTime = rolePermission.ExpirationTime,
            GrantReason = rolePermission.GrantReason,
            Status = rolePermission.Status,
            Remark = rolePermission.Remark,
            CreatedTime = rolePermission.CreatedTime
        };
    }

    /// <summary>
    /// 映射角色权限详情
    /// </summary>
    /// <param name="rolePermission">角色权限绑定</param>
    /// <param name="permission">权限定义</param>
    /// <returns>角色权限详情 DTO</returns>
    public static RolePermissionDetailDto ToDetailDto(SysRolePermission rolePermission, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(rolePermission);

        return new RolePermissionDetailDto
        {
            BasicId = rolePermission.BasicId,
            RoleId = rolePermission.RoleId,
            PermissionId = rolePermission.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            PermissionDescription = permission?.PermissionDescription,
            PermissionType = permission?.PermissionType,
            ModuleCode = permission?.ModuleCode,
            Tags = permission?.Tags,
            IsGlobalPermission = permission?.IsGlobal,
            IsRequireAudit = permission?.IsRequireAudit,
            PermissionPriority = permission?.Priority,
            PermissionStatus = permission?.Status,
            PermissionAction = rolePermission.PermissionAction,
            EffectiveTime = rolePermission.EffectiveTime,
            ExpirationTime = rolePermission.ExpirationTime,
            GrantReason = rolePermission.GrantReason,
            Status = rolePermission.Status,
            Remark = rolePermission.Remark,
            CreatedTime = rolePermission.CreatedTime,
            CreatedId = rolePermission.CreatedId,
            CreatedBy = rolePermission.CreatedBy
        };
    }
}
