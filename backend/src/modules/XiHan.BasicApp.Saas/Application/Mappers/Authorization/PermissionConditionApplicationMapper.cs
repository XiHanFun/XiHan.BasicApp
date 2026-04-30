#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionApplicationMapper
// Guid:69b3b8d7-7f8b-4898-aa7f-cc847af57b39
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限 ABAC 条件应用层映射器
/// </summary>
public static class PermissionConditionApplicationMapper
{
    /// <summary>
    /// 映射权限 ABAC 条件列表项
    /// </summary>
    public static PermissionConditionListItemDto ToListItemDto(
        SysPermissionCondition condition,
        SysRolePermission? rolePermission,
        SysUserPermission? userPermission,
        SysPermission? permission,
        SysRole? role,
        SysTenantUser? tenantMember)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new PermissionConditionListItemDto
        {
            BasicId = condition.BasicId,
            RolePermissionId = condition.RolePermissionId,
            UserPermissionId = condition.UserPermissionId,
            RoleId = rolePermission?.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            UserId = userPermission?.UserId,
            UserDisplayName = tenantMember?.DisplayName,
            PermissionId = rolePermission?.PermissionId ?? userPermission?.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            ConditionGroup = condition.ConditionGroup,
            AttributeName = condition.AttributeName,
            Operator = condition.Operator,
            IsNegated = condition.IsNegated,
            ValueType = condition.ValueType,
            ConditionValue = condition.ConditionValue,
            Description = condition.Description,
            Status = condition.Status,
            Remark = condition.Remark,
            CreatedTime = condition.CreatedTime
        };
    }

    /// <summary>
    /// 映射权限 ABAC 条件详情
    /// </summary>
    public static PermissionConditionDetailDto ToDetailDto(
        SysPermissionCondition condition,
        SysRolePermission? rolePermission,
        SysUserPermission? userPermission,
        SysPermission? permission,
        SysRole? role,
        SysTenantUser? tenantMember)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new PermissionConditionDetailDto
        {
            BasicId = condition.BasicId,
            RolePermissionId = condition.RolePermissionId,
            UserPermissionId = condition.UserPermissionId,
            RoleId = rolePermission?.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            UserId = userPermission?.UserId,
            UserDisplayName = tenantMember?.DisplayName,
            PermissionId = rolePermission?.PermissionId ?? userPermission?.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            ConditionGroup = condition.ConditionGroup,
            AttributeName = condition.AttributeName,
            Operator = condition.Operator,
            IsNegated = condition.IsNegated,
            ValueType = condition.ValueType,
            ConditionValue = condition.ConditionValue,
            Description = condition.Description,
            Status = condition.Status,
            Remark = condition.Remark,
            CreatedTime = condition.CreatedTime,
            CreatedId = condition.CreatedId,
            CreatedBy = condition.CreatedBy,
            ModifiedTime = condition.ModifiedTime,
            ModifiedId = condition.ModifiedId,
            ModifiedBy = condition.ModifiedBy
        };
    }
}
