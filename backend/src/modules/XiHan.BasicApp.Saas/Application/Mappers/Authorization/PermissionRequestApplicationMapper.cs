#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestApplicationMapper
// Guid:788a34ce-a5b1-4640-93c0-4f25ce360b82
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限申请应用层映射器
/// </summary>
public static class PermissionRequestApplicationMapper
{
    /// <summary>
    /// 映射权限申请列表项
    /// </summary>
    public static PermissionRequestListItemDto ToListItemDto(
        SysPermissionRequest request,
        SysTenantUser? requestUser,
        SysPermission? permission,
        SysRole? role,
        SysReview? review,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PermissionRequestListItemDto
        {
            BasicId = request.BasicId,
            RequestUserId = request.RequestUserId,
            RequestTenantMemberId = requestUser?.BasicId,
            RequestUserDisplayName = requestUser?.DisplayName,
            PermissionId = request.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            RoleId = request.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            RequestStatus = request.RequestStatus,
            RequestReason = request.RequestReason,
            ExpectedEffectiveTime = request.ExpectedEffectiveTime,
            ExpectedExpirationTime = request.ExpectedExpirationTime,
            IsExpectedExpired = request.ExpectedExpirationTime.HasValue && request.ExpectedExpirationTime.Value <= now,
            ReviewId = request.ReviewId,
            ReviewCode = review?.ReviewCode,
            ReviewTitle = review?.ReviewTitle,
            ReviewStatus = review?.ReviewStatus,
            ReviewResult = review?.ReviewResult,
            CreatedTime = request.CreatedTime,
            ModifiedTime = request.ModifiedTime
        };
    }

    /// <summary>
    /// 映射权限申请详情
    /// </summary>
    public static PermissionRequestDetailDto ToDetailDto(
        SysPermissionRequest request,
        SysTenantUser? requestUser,
        SysPermission? permission,
        SysRole? role,
        SysReview? review,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PermissionRequestDetailDto
        {
            BasicId = request.BasicId,
            RequestUserId = request.RequestUserId,
            RequestTenantMemberId = requestUser?.BasicId,
            RequestUserDisplayName = requestUser?.DisplayName,
            PermissionId = request.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            PermissionDescription = permission?.PermissionDescription,
            RoleId = request.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            RoleDescription = role?.RoleDescription,
            RequestStatus = request.RequestStatus,
            RequestReason = request.RequestReason,
            ExpectedEffectiveTime = request.ExpectedEffectiveTime,
            ExpectedExpirationTime = request.ExpectedExpirationTime,
            IsExpectedExpired = request.ExpectedExpirationTime.HasValue && request.ExpectedExpirationTime.Value <= now,
            ReviewId = request.ReviewId,
            ReviewCode = review?.ReviewCode,
            ReviewTitle = review?.ReviewTitle,
            ReviewDescription = review?.ReviewDescription,
            ReviewStatus = review?.ReviewStatus,
            ReviewResult = review?.ReviewResult,
            Remark = request.Remark,
            CreatedTime = request.CreatedTime,
            CreatedId = request.CreatedId,
            CreatedBy = request.CreatedBy,
            ModifiedTime = request.ModifiedTime,
            ModifiedId = request.ModifiedId,
            ModifiedBy = request.ModifiedBy
        };
    }
}
