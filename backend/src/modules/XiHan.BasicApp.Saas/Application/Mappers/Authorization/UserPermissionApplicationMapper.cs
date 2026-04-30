#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionApplicationMapper
// Guid:d8c09299-8641-45ec-a263-05c5784c50d5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户直授权限应用层映射器
/// </summary>
public static class UserPermissionApplicationMapper
{
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
}
