// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
            input.Remark,
            input.Side);
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

        var groupCode = SaasPermissionDefinitions.ResolveGroupCode(permission.PermissionCode);

        // 显示名只取真正的名字：Saas 定义表里的组名，其次是资源表里的中文名（AI / 代码生成 / 工作流挂了资源）。
        // 都没有时留空——聊天、打印这类只有功能权限、不挂资源的模块，把组码当名字就会显示成 chat、print-template；
        // 留空后前端按组内权限名的公共前缀命名（聊天查看 / 聊天发送 → 聊天），不必让 Saas 反过来认识各模块的权限码。
        var groupName = SaasPermissionDefinitions.GroupNames.TryGetValue(groupCode, out var definedName)
            ? definedName
            : resource?.ResourceName?.Trim() ?? string.Empty;

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
            GroupCode = groupCode,
            GroupName = groupName,
            PermissionDescription = permission.PermissionDescription,
            IsRequireAudit = permission.IsRequireAudit,
            Side = permission.Side,
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
            Side = permission.Side,
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
            Side = permission.Side,
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
            input.Remark,
            input.Side);
    }
}
