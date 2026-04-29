#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDataScopeApplicationMapper
// Guid:3d944b4a-aab8-4e44-9d60-b55144d5426c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色数据范围应用层映射器
/// </summary>
public static class RoleDataScopeApplicationMapper
{
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
}
