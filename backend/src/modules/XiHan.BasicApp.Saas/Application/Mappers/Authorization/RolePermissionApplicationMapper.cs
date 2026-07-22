// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色权限应用层映射器
/// </summary>
public static class RolePermissionApplicationMapper
{
    /// <summary>
    /// 映射角色权限授权命令
    /// </summary>
    public static RolePermissionGrantCommand ToGrantCommand(RolePermissionGrantDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new RolePermissionGrantCommand(
            input.RoleId,
            input.PermissionId,
            input.PermissionAction,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

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

    /// <summary>
    /// 映射角色权限状态变更命令
    /// </summary>
    public static RolePermissionStatusChangeCommand ToStatusCommand(RolePermissionStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new RolePermissionStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射角色权限更新命令
    /// </summary>
    public static RolePermissionUpdateCommand ToUpdateCommand(RolePermissionUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new RolePermissionUpdateCommand(
            input.BasicId,
            input.PermissionAction,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }
}
