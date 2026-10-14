// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限申请应用层映射器
/// </summary>
public static class PermissionRequestApplicationMapper
{
    /// <summary>
    /// 映射权限申请创建命令
    /// </summary>
    public static PermissionRequestCreateCommand ToCreateCommand(PermissionRequestCreateDto input, long requestUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionRequestCreateCommand(
            requestUserId,
            input.PermissionId,
            input.RoleId,
            input.RequestReason,
            input.ExpectedEffectiveTime,
            input.ExpectedExpirationTime,
            input.Remark);
    }

    /// <summary>
    /// 映射权限申请列表项
    /// </summary>
    public static PermissionRequestListItemDto ToListItemDto(
        SysPermissionRequest request,
        SysTenantUser? requestUser,
        SysPermission? permission,
        SysRole? role,
        WorkflowBusinessProgress? workflow,
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
            WorkflowInstanceId = request.WorkflowInstanceId,
            Workflow = workflow is null ? null : WorkflowProgressApplicationMapper.ToDto(workflow),
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
        WorkflowBusinessProgress? workflow,
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
            WorkflowInstanceId = request.WorkflowInstanceId,
            Workflow = workflow is null ? null : WorkflowProgressApplicationMapper.ToDto(workflow),
            Remark = request.Remark,
            CreatedTime = request.CreatedTime,
            CreatedId = request.CreatedId,
            CreatedBy = request.CreatedBy,
            ModifiedTime = request.ModifiedTime,
            ModifiedId = request.ModifiedId,
            ModifiedBy = request.ModifiedBy
        };
    }

    /// <summary>
    /// 映射权限申请状态变更命令
    /// </summary>
    public static PermissionRequestStatusCommand ToStatusCommand(PermissionRequestStatusUpdateDto input, long operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionRequestStatusCommand(
            input.BasicId,
            operatorUserId,
            input.RequestStatus,
            input.Remark);
    }

    /// <summary>
    /// 映射权限申请更新命令
    /// </summary>
    public static PermissionRequestUpdateCommand ToUpdateCommand(PermissionRequestUpdateDto input, long requestUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionRequestUpdateCommand(
            input.BasicId,
            requestUserId,
            input.PermissionId,
            input.RoleId,
            input.RequestReason,
            input.ExpectedEffectiveTime,
            input.ExpectedExpirationTime,
            input.Remark);
    }
}
