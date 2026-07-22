// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限 ABAC 条件应用层映射器
/// </summary>
public static class PermissionConditionApplicationMapper
{
    /// <summary>
    /// 映射权限 ABAC 条件创建命令
    /// </summary>
    public static PermissionConditionCreateCommand ToCreateCommand(PermissionConditionCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionConditionCreateCommand(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName,
            input.Operator,
            input.IsNegated,
            input.ValueType,
            input.ConditionValue,
            input.Description,
            input.Status,
            input.Remark);
    }

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

    /// <summary>
    /// 映射权限 ABAC 条件状态变更命令
    /// </summary>
    public static PermissionConditionStatusCommand ToStatusCommand(PermissionConditionStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new PermissionConditionStatusCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射权限 ABAC 条件更新命令
    /// </summary>
    public static PermissionConditionUpdateCommand ToUpdateCommand(PermissionConditionUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionConditionUpdateCommand(
            input.BasicId,
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName,
            input.Operator,
            input.IsNegated,
            input.ValueType,
            input.ConditionValue,
            input.Description,
            input.Remark);
    }
}
