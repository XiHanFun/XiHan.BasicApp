#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentApplicationMapper
// Guid:c31640ae-7700-43f9-9dd5-13c60a269c6c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户部门归属应用层映射器
/// </summary>
public static class UserDepartmentApplicationMapper
{
    /// <summary>
    /// 映射用户部门归属列表项
    /// </summary>
    /// <param name="userDepartment">用户部门归属</param>
    /// <param name="department">部门</param>
    /// <returns>用户部门归属列表项 DTO</returns>
    public static UserDepartmentListItemDto ToListItemDto(SysUserDepartment userDepartment, SysDepartment? department)
    {
        ArgumentNullException.ThrowIfNull(userDepartment);

        return new UserDepartmentListItemDto
        {
            BasicId = userDepartment.BasicId,
            UserId = userDepartment.UserId,
            DepartmentId = userDepartment.DepartmentId,
            DepartmentCode = department?.DepartmentCode,
            DepartmentName = department?.DepartmentName,
            ParentId = department?.ParentId,
            DepartmentType = department?.DepartmentType,
            DepartmentStatus = department?.Status,
            IsMain = userDepartment.IsMain,
            Status = userDepartment.Status,
            Remark = userDepartment.Remark,
            CreatedTime = userDepartment.CreatedTime
        };
    }

    /// <summary>
    /// 映射用户部门归属详情
    /// </summary>
    /// <param name="userDepartment">用户部门归属</param>
    /// <param name="department">部门</param>
    /// <returns>用户部门归属详情 DTO</returns>
    public static UserDepartmentDetailDto ToDetailDto(SysUserDepartment userDepartment, SysDepartment? department)
    {
        ArgumentNullException.ThrowIfNull(userDepartment);

        return new UserDepartmentDetailDto
        {
            BasicId = userDepartment.BasicId,
            UserId = userDepartment.UserId,
            DepartmentId = userDepartment.DepartmentId,
            DepartmentCode = department?.DepartmentCode,
            DepartmentName = department?.DepartmentName,
            ParentId = department?.ParentId,
            DepartmentType = department?.DepartmentType,
            DepartmentStatus = department?.Status,
            IsMain = userDepartment.IsMain,
            Status = userDepartment.Status,
            Remark = userDepartment.Remark,
            CreatedTime = userDepartment.CreatedTime,
            CreatedId = userDepartment.CreatedId,
            CreatedBy = userDepartment.CreatedBy
        };
    }
}
