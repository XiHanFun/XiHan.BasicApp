// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户角色应用层映射器
/// </summary>
public static class UserRoleApplicationMapper
{
    /// <summary>
    /// 映射用户角色授权命令
    /// </summary>
    public static UserRoleGrantCommand ToGrantCommand(UserRoleGrantDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserRoleGrantCommand(
            input.UserId,
            input.RoleId,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

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

    /// <summary>
    /// 映射用户角色状态变更命令
    /// </summary>
    public static UserRoleStatusChangeCommand ToStatusCommand(UserRoleStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new UserRoleStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射用户角色更新命令
    /// </summary>
    public static UserRoleUpdateCommand ToUpdateCommand(UserRoleUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserRoleUpdateCommand(
            input.BasicId,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }
}
