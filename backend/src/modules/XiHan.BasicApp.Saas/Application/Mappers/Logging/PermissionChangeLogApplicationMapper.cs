// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限变更日志应用层映射器
/// </summary>
public static class PermissionChangeLogApplicationMapper
{
    /// <summary>
    /// 映射权限变更日志列表项
    /// </summary>
    /// <param name="permissionChangeLog">权限变更日志实体</param>
    /// <returns>权限变更日志列表项 DTO</returns>
    public static PermissionChangeLogListItemDto ToListItemDto(SysPermissionChangeLog permissionChangeLog)
    {
        ArgumentNullException.ThrowIfNull(permissionChangeLog);

        return new PermissionChangeLogListItemDto
        {
            BasicId = permissionChangeLog.BasicId,
            OperatorUserId = permissionChangeLog.OperatorUserId,
            OperatorUserName = permissionChangeLog.OperatorUserName,
            TargetUserId = permissionChangeLog.TargetUserId,
            TargetUserName = permissionChangeLog.TargetUserName,
            TargetRoleId = permissionChangeLog.TargetRoleId,
            TargetRoleName = permissionChangeLog.TargetRoleName,
            PermissionId = permissionChangeLog.PermissionId,
            PermissionName = permissionChangeLog.PermissionName,
            ChangeType = permissionChangeLog.ChangeType,
            ChangeReason = permissionChangeLog.ChangeReason,
            Description = permissionChangeLog.Description,
            OperationIp = permissionChangeLog.OperationIp,
            TraceId = permissionChangeLog.TraceId,
            ChangeTime = permissionChangeLog.ChangeTime,
            CreatedTime = permissionChangeLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射权限变更日志详情
    /// </summary>
    /// <param name="permissionChangeLog">权限变更日志实体</param>
    /// <returns>权限变更日志详情 DTO</returns>
    public static PermissionChangeLogDetailDto ToDetailDto(SysPermissionChangeLog permissionChangeLog)
    {
        ArgumentNullException.ThrowIfNull(permissionChangeLog);

        var item = ToListItemDto(permissionChangeLog);
        return new PermissionChangeLogDetailDto
        {
            BasicId = item.BasicId,
            OperatorUserId = item.OperatorUserId,
            OperatorUserName = item.OperatorUserName,
            TargetUserId = item.TargetUserId,
            TargetUserName = item.TargetUserName,
            TargetRoleId = item.TargetRoleId,
            TargetRoleName = item.TargetRoleName,
            PermissionId = item.PermissionId,
            PermissionName = item.PermissionName,
            ChangeType = item.ChangeType,
            ChangeReason = item.ChangeReason,
            Description = item.Description,
            OperationIp = item.OperationIp,
            TraceId = item.TraceId,
            ChangeTime = item.ChangeTime,
            CreatedTime = item.CreatedTime,
            CreatedId = permissionChangeLog.CreatedId,
            CreatedBy = permissionChangeLog.CreatedBy
        };
    }
}
