#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionApplicationMapper
// Guid:ed30cf55-e834-436d-af0a-d477f6f591b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 权限应用层映射器
/// </summary>
public static class PermissionApplicationMapper
{
    /// <summary>
    /// 映射权限创建命令
    /// </summary>
    public static PermissionCreateCommand ToCreateCommand(PermissionCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionCreateCommand(
            input.PermissionType,
            input.ResourceId,
            input.OperationId,
            input.ModuleCode,
            input.PermissionCode,
            input.PermissionName,
            input.PermissionDescription,
            input.Tags,
            input.IsRequireAudit,
            input.Priority,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射权限列表项
    /// </summary>
    /// <param name="permission">权限定义</param>
    /// <param name="resource">资源定义</param>
    /// <param name="operation">操作定义</param>
    /// <returns>权限列表项 DTO</returns>
    public static PermissionListItemDto ToListItemDto(SysPermission permission, SysResource? resource, SysOperation? operation)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionListItemDto
        {
            BasicId = permission.BasicId,
            PermissionType = permission.PermissionType,
            ResourceId = permission.ResourceId,
            ResourceCode = resource?.ResourceCode,
            ResourceName = resource?.ResourceName,
            OperationId = permission.OperationId,
            OperationCode = operation?.OperationCode,
            OperationName = operation?.OperationName,
            ModuleCode = permission.ModuleCode,
            PermissionCode = permission.PermissionCode,
            PermissionName = permission.PermissionName,
            GroupCode = SaasPermissionDefinitions.ResolveGroupCode(permission.PermissionCode),
            GroupName = SaasPermissionDefinitions.ResolveGroupName(permission.PermissionCode),
            PermissionDescription = permission.PermissionDescription,
            IsRequireAudit = permission.IsRequireAudit,
            IsGlobal = permission.IsGlobal,
            Priority = permission.Priority,
            Status = permission.Status,
            Sort = permission.Sort,
            CreatedTime = permission.CreatedTime,
            ModifiedTime = permission.ModifiedTime
        };
    }

    /// <summary>
    /// 映射权限详情
    /// </summary>
    /// <param name="permission">权限定义</param>
    /// <param name="resource">资源定义</param>
    /// <param name="operation">操作定义</param>
    /// <returns>权限详情 DTO</returns>
    public static PermissionDetailDto ToDetailDto(SysPermission permission, SysResource? resource, SysOperation? operation)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionDetailDto
        {
            BasicId = permission.BasicId,
            PermissionType = permission.PermissionType,
            ResourceId = permission.ResourceId,
            ResourceCode = resource?.ResourceCode,
            ResourceName = resource?.ResourceName,
            OperationId = permission.OperationId,
            OperationCode = operation?.OperationCode,
            OperationName = operation?.OperationName,
            ModuleCode = permission.ModuleCode,
            PermissionCode = permission.PermissionCode,
            PermissionName = permission.PermissionName,
            PermissionDescription = permission.PermissionDescription,
            Tags = permission.Tags,
            IsRequireAudit = permission.IsRequireAudit,
            IsGlobal = permission.IsGlobal,
            Priority = permission.Priority,
            Status = permission.Status,
            Sort = permission.Sort,
            Remark = permission.Remark,
            CreatedTime = permission.CreatedTime,
            CreatedId = permission.CreatedId,
            CreatedBy = permission.CreatedBy,
            ModifiedTime = permission.ModifiedTime,
            ModifiedId = permission.ModifiedId,
            ModifiedBy = permission.ModifiedBy
        };
    }

    /// <summary>
    /// 映射权限选择项
    /// </summary>
    /// <param name="permission">权限定义</param>
    /// <returns>权限选择项 DTO</returns>
    public static PermissionSelectItemDto ToSelectItemDto(SysPermission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionSelectItemDto
        {
            BasicId = permission.BasicId,
            PermissionType = permission.PermissionType,
            ModuleCode = permission.ModuleCode,
            PermissionCode = permission.PermissionCode,
            PermissionName = permission.PermissionName,
            IsRequireAudit = permission.IsRequireAudit,
            Priority = permission.Priority
        };
    }

    /// <summary>
    /// 映射权限状态变更命令
    /// </summary>
    public static PermissionStatusCommand ToStatusCommand(PermissionStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new PermissionStatusCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射权限更新命令
    /// </summary>
    public static PermissionUpdateCommand ToUpdateCommand(PermissionUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PermissionUpdateCommand(
            input.BasicId,
            input.PermissionName,
            input.PermissionDescription,
            input.Tags,
            input.IsRequireAudit,
            input.Priority,
            input.Sort,
            input.Remark);
    }
}
