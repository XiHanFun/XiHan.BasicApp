// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户直授权限应用层映射器
/// </summary>
public static class UserPermissionApplicationMapper
{
    /// <summary>
    /// 映射用户直授权限授权命令
    /// </summary>
    public static UserPermissionGrantCommand ToGrantCommand(UserPermissionGrantDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserPermissionGrantCommand(
            input.UserId,
            input.PermissionId,
            input.PermissionAction,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

    /// <summary>
    /// 映射用户直授权限列表项
    /// </summary>
    /// <param name="userPermission">用户直授权限绑定</param>
    /// <param name="permission">权限定义</param>
    /// <param name="tenantMember">租户成员</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户直授权限列表项 DTO</returns>
    public static UserPermissionListItemDto ToListItemDto(SysUserPermission userPermission, SysPermission? permission, SysTenantUser? tenantMember, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(userPermission);

        return new UserPermissionListItemDto
        {
            BasicId = userPermission.BasicId,
            UserId = userPermission.UserId,
            TenantMemberId = tenantMember?.BasicId,
            TenantMemberDisplayName = tenantMember?.DisplayName,
            TenantMemberType = tenantMember?.MemberType,
            TenantMemberInviteStatus = tenantMember?.InviteStatus,
            TenantMemberStatus = tenantMember?.Status,
            PermissionId = userPermission.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            PermissionType = permission?.PermissionType,
            ModuleCode = permission?.ModuleCode,
            IsGlobalPermission = permission?.IsGlobal,
            IsRequireAudit = permission?.IsRequireAudit,
            PermissionStatus = permission?.Status,
            PermissionAction = userPermission.PermissionAction,
            EffectiveTime = userPermission.EffectiveTime,
            ExpirationTime = userPermission.ExpirationTime,
            GrantReason = userPermission.GrantReason,
            Status = userPermission.Status,
            IsExpired = userPermission.ExpirationTime.HasValue && userPermission.ExpirationTime.Value <= now,
            Remark = userPermission.Remark,
            CreatedTime = userPermission.CreatedTime
        };
    }

    /// <summary>
    /// 映射用户直授权限详情
    /// </summary>
    /// <param name="userPermission">用户直授权限绑定</param>
    /// <param name="permission">权限定义</param>
    /// <param name="tenantMember">租户成员</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户直授权限详情 DTO</returns>
    public static UserPermissionDetailDto ToDetailDto(SysUserPermission userPermission, SysPermission? permission, SysTenantUser? tenantMember, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(userPermission);

        return new UserPermissionDetailDto
        {
            BasicId = userPermission.BasicId,
            UserId = userPermission.UserId,
            TenantMemberId = tenantMember?.BasicId,
            TenantMemberDisplayName = tenantMember?.DisplayName,
            TenantMemberType = tenantMember?.MemberType,
            TenantMemberInviteStatus = tenantMember?.InviteStatus,
            TenantMemberStatus = tenantMember?.Status,
            PermissionId = userPermission.PermissionId,
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
            PermissionAction = userPermission.PermissionAction,
            EffectiveTime = userPermission.EffectiveTime,
            ExpirationTime = userPermission.ExpirationTime,
            GrantReason = userPermission.GrantReason,
            Status = userPermission.Status,
            IsExpired = userPermission.ExpirationTime.HasValue && userPermission.ExpirationTime.Value <= now,
            Remark = userPermission.Remark,
            CreatedTime = userPermission.CreatedTime,
            CreatedId = userPermission.CreatedId,
            CreatedBy = userPermission.CreatedBy
        };
    }

    /// <summary>
    /// 映射用户直授权限状态变更命令
    /// </summary>
    public static UserPermissionStatusChangeCommand ToStatusCommand(UserPermissionStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new UserPermissionStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射用户直授权限更新命令
    /// </summary>
    public static UserPermissionUpdateCommand ToUpdateCommand(UserPermissionUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserPermissionUpdateCommand(
            input.BasicId,
            input.PermissionAction,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }
}
