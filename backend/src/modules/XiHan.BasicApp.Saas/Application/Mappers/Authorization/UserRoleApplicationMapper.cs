#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRoleApplicationMapper
// Guid:5a31d9e2-f034-40ed-8f78-9ff4f35b7fb4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户角色应用层映射器
/// </summary>
public static class UserRoleApplicationMapper
{
    /// <summary>
    /// 映射用户角色列表项
    /// </summary>
    /// <param name="userRole">用户角色绑定</param>
    /// <param name="role">角色</param>
    /// <param name="tenantMember">租户成员</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户角色列表项 DTO</returns>
    public static UserRoleListItemDto ToListItemDto(SysUserRole userRole, SysRole? role, SysTenantUser? tenantMember, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(userRole);

        return new UserRoleListItemDto
        {
            BasicId = userRole.BasicId,
            UserId = userRole.UserId,
            TenantMemberId = tenantMember?.BasicId,
            TenantMemberDisplayName = tenantMember?.DisplayName,
            TenantMemberType = tenantMember?.MemberType,
            TenantMemberInviteStatus = tenantMember?.InviteStatus,
            TenantMemberStatus = tenantMember?.Status,
            RoleId = userRole.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            RoleType = role?.RoleType,
            IsGlobalRole = role?.IsGlobal,
            RoleDataScope = role?.DataScope,
            RoleStatus = role?.Status,
            EffectiveTime = userRole.EffectiveTime,
            ExpirationTime = userRole.ExpirationTime,
            GrantReason = userRole.GrantReason,
            Status = userRole.Status,
            IsExpired = userRole.ExpirationTime.HasValue && userRole.ExpirationTime.Value <= now,
            Remark = userRole.Remark,
            CreatedTime = userRole.CreatedTime
        };
    }

    /// <summary>
    /// 映射用户角色详情
    /// </summary>
    /// <param name="userRole">用户角色绑定</param>
    /// <param name="role">角色</param>
    /// <param name="tenantMember">租户成员</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户角色详情 DTO</returns>
    public static UserRoleDetailDto ToDetailDto(SysUserRole userRole, SysRole? role, SysTenantUser? tenantMember, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(userRole);

        return new UserRoleDetailDto
        {
            BasicId = userRole.BasicId,
            UserId = userRole.UserId,
            TenantMemberId = tenantMember?.BasicId,
            TenantMemberDisplayName = tenantMember?.DisplayName,
            TenantMemberType = tenantMember?.MemberType,
            TenantMemberInviteStatus = tenantMember?.InviteStatus,
            TenantMemberStatus = tenantMember?.Status,
            RoleId = userRole.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            RoleDescription = role?.RoleDescription,
            RoleType = role?.RoleType,
            IsGlobalRole = role?.IsGlobal,
            RoleDataScope = role?.DataScope,
            RoleStatus = role?.Status,
            EffectiveTime = userRole.EffectiveTime,
            ExpirationTime = userRole.ExpirationTime,
            GrantReason = userRole.GrantReason,
            Status = userRole.Status,
            IsExpired = userRole.ExpirationTime.HasValue && userRole.ExpirationTime.Value <= now,
            Remark = userRole.Remark,
            CreatedTime = userRole.CreatedTime,
            CreatedId = userRole.CreatedId,
            CreatedBy = userRole.CreatedBy
        };
    }
}
