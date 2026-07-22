// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色数据范围应用层映射器
/// </summary>
public static class RoleDataScopeApplicationMapper
{
    /// <summary>
    /// 映射角色数据范围授权命令
    /// </summary>
    public static RoleDataScopeGrantCommand ToGrantCommand(RoleDataScopeGrantDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new RoleDataScopeGrantCommand(
            input.RoleId,
            input.DepartmentId,
            input.IncludeChildren,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark);
    }

    /// <summary>
    /// 映射角色数据范围列表项
    /// </summary>
    /// <param name="scope">角色数据范围</param>
    /// <param name="department">部门</param>
    /// <returns>角色数据范围列表项 DTO</returns>
    public static RoleDataScopeListItemDto ToListItemDto(SysRoleDataScope scope, SysDepartment? department)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new RoleDataScopeListItemDto
        {
            BasicId = scope.BasicId,
            RoleId = scope.RoleId,
            DepartmentId = scope.DepartmentId,
            DepartmentCode = department?.DepartmentCode,
            DepartmentName = department?.DepartmentName,
            ParentId = department?.ParentId,
            DepartmentType = department?.DepartmentType,
            DepartmentStatus = department?.Status,
            IncludeChildren = scope.IncludeChildren,
            EffectiveTime = scope.EffectiveTime,
            ExpirationTime = scope.ExpirationTime,
            Status = scope.Status,
            Remark = scope.Remark,
            CreatedTime = scope.CreatedTime
        };
    }

    /// <summary>
    /// 映射角色数据范围详情
    /// </summary>
    /// <param name="scope">角色数据范围</param>
    /// <param name="department">部门</param>
    /// <returns>角色数据范围详情 DTO</returns>
    public static RoleDataScopeDetailDto ToDetailDto(SysRoleDataScope scope, SysDepartment? department)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new RoleDataScopeDetailDto
        {
            BasicId = scope.BasicId,
            RoleId = scope.RoleId,
            DepartmentId = scope.DepartmentId,
            DepartmentCode = department?.DepartmentCode,
            DepartmentName = department?.DepartmentName,
            ParentId = department?.ParentId,
            DepartmentType = department?.DepartmentType,
            DepartmentStatus = department?.Status,
            IncludeChildren = scope.IncludeChildren,
            EffectiveTime = scope.EffectiveTime,
            ExpirationTime = scope.ExpirationTime,
            Status = scope.Status,
            Remark = scope.Remark,
            CreatedTime = scope.CreatedTime,
            CreatedId = scope.CreatedId,
            CreatedBy = scope.CreatedBy
        };
    }

    /// <summary>
    /// 映射角色数据范围状态变更命令
    /// </summary>
    public static RoleDataScopeStatusChangeCommand ToStatusCommand(RoleDataScopeStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new RoleDataScopeStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射角色数据范围更新命令
    /// </summary>
    public static RoleDataScopeUpdateCommand ToUpdateCommand(RoleDataScopeUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new RoleDataScopeUpdateCommand(
            input.BasicId,
            input.IncludeChildren,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark);
    }
}
