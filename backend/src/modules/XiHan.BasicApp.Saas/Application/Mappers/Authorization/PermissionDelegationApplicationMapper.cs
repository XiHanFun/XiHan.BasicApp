#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationApplicationMapper
// Guid:a24f9b02-2824-41c6-9823-0a8a30d84c34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限委托应用层映射器
/// </summary>
public static class PermissionDelegationApplicationMapper
{
    /// <summary>
    /// 映射权限委托列表项
    /// </summary>
    public static PermissionDelegationListItemDto ToListItemDto(
        SysPermissionDelegation delegation,
        SysTenantUser? delegator,
        SysTenantUser? delegatee,
        SysPermission? permission,
        SysRole? role,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(delegation);

        return new PermissionDelegationListItemDto
        {
            BasicId = delegation.BasicId,
            DelegatorUserId = delegation.DelegatorUserId,
            DelegatorTenantMemberId = delegator?.BasicId,
            DelegatorDisplayName = delegator?.DisplayName,
            DelegateeUserId = delegation.DelegateeUserId,
            DelegateeTenantMemberId = delegatee?.BasicId,
            DelegateeDisplayName = delegatee?.DisplayName,
            PermissionId = delegation.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            RoleId = delegation.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            DelegationStatus = delegation.DelegationStatus,
            EffectiveTime = delegation.EffectiveTime,
            ExpirationTime = delegation.ExpirationTime,
            IsExpired = delegation.ExpirationTime <= now,
            DelegationReason = delegation.DelegationReason,
            CreatedTime = delegation.CreatedTime,
            ModifiedTime = delegation.ModifiedTime
        };
    }

    /// <summary>
    /// 映射权限委托详情
    /// </summary>
    public static PermissionDelegationDetailDto ToDetailDto(
        SysPermissionDelegation delegation,
        SysTenantUser? delegator,
        SysTenantUser? delegatee,
        SysPermission? permission,
        SysRole? role,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(delegation);

        return new PermissionDelegationDetailDto
        {
            BasicId = delegation.BasicId,
            DelegatorUserId = delegation.DelegatorUserId,
            DelegatorTenantMemberId = delegator?.BasicId,
            DelegatorDisplayName = delegator?.DisplayName,
            DelegateeUserId = delegation.DelegateeUserId,
            DelegateeTenantMemberId = delegatee?.BasicId,
            DelegateeDisplayName = delegatee?.DisplayName,
            PermissionId = delegation.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            PermissionDescription = permission?.PermissionDescription,
            RoleId = delegation.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            RoleDescription = role?.RoleDescription,
            DelegationStatus = delegation.DelegationStatus,
            EffectiveTime = delegation.EffectiveTime,
            ExpirationTime = delegation.ExpirationTime,
            IsExpired = delegation.ExpirationTime <= now,
            DelegationReason = delegation.DelegationReason,
            Remark = delegation.Remark,
            CreatedTime = delegation.CreatedTime,
            CreatedId = delegation.CreatedId,
            CreatedBy = delegation.CreatedBy,
            ModifiedTime = delegation.ModifiedTime,
            ModifiedId = delegation.ModifiedId,
            ModifiedBy = delegation.ModifiedBy
        };
    }
}
