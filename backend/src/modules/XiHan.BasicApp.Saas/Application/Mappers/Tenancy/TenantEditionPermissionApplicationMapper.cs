#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionPermissionApplicationMapper
// Guid:b49ff906-88bd-45b9-a274-b5901527ed85
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 租户版本权限应用层映射器
/// </summary>
public static class TenantEditionPermissionApplicationMapper
{
    /// <summary>
    /// 映射租户版本权限列表项
    /// </summary>
    /// <param name="editionPermission">租户版本权限绑定</param>
    /// <param name="permission">权限定义</param>
    /// <returns>租户版本权限列表项 DTO</returns>
    public static TenantEditionPermissionListItemDto ToListItemDto(SysTenantEditionPermission editionPermission, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(editionPermission);

        return new TenantEditionPermissionListItemDto
        {
            BasicId = editionPermission.BasicId,
            EditionId = editionPermission.EditionId,
            PermissionId = editionPermission.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            PermissionType = permission?.PermissionType,
            ModuleCode = permission?.ModuleCode,
            IsGlobalPermission = permission?.IsGlobal,
            IsRequireAudit = permission?.IsRequireAudit,
            PermissionStatus = permission?.Status,
            Status = editionPermission.Status,
            Remark = editionPermission.Remark,
            CreatedTime = editionPermission.CreatedTime
        };
    }

    /// <summary>
    /// 映射租户版本权限详情
    /// </summary>
    /// <param name="editionPermission">租户版本权限绑定</param>
    /// <param name="permission">权限定义</param>
    /// <returns>租户版本权限详情 DTO</returns>
    public static TenantEditionPermissionDetailDto ToDetailDto(SysTenantEditionPermission editionPermission, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(editionPermission);

        return new TenantEditionPermissionDetailDto
        {
            BasicId = editionPermission.BasicId,
            EditionId = editionPermission.EditionId,
            PermissionId = editionPermission.PermissionId,
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
            Status = editionPermission.Status,
            Remark = editionPermission.Remark,
            CreatedTime = editionPermission.CreatedTime,
            CreatedId = editionPermission.CreatedId,
            CreatedBy = editionPermission.CreatedBy
        };
    }
}
