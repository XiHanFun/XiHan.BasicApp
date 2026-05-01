#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionChangeLogApplicationMapper
// Guid:9660d3e1-4b10-4d52-b5b3-3280a8ac0b71
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
            TargetUserId = permissionChangeLog.TargetUserId,
            TargetRoleId = permissionChangeLog.TargetRoleId,
            PermissionId = permissionChangeLog.PermissionId,
            ChangeType = permissionChangeLog.ChangeType,
            TraceId = permissionChangeLog.TraceId,
            ChangeTime = permissionChangeLog.ChangeTime,
            HasReasonNote = !string.IsNullOrWhiteSpace(permissionChangeLog.ChangeReason),
            HasChangeSummary = !string.IsNullOrWhiteSpace(permissionChangeLog.Description),
            HasOperationContext = !string.IsNullOrWhiteSpace(permissionChangeLog.OperationIp),
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
            TargetUserId = item.TargetUserId,
            TargetRoleId = item.TargetRoleId,
            PermissionId = item.PermissionId,
            ChangeType = item.ChangeType,
            TraceId = item.TraceId,
            ChangeTime = item.ChangeTime,
            HasReasonNote = item.HasReasonNote,
            HasChangeSummary = item.HasChangeSummary,
            HasOperationContext = item.HasOperationContext,
            CreatedTime = item.CreatedTime,
            CreatedId = permissionChangeLog.CreatedId,
            CreatedBy = permissionChangeLog.CreatedBy
        };
    }
}
