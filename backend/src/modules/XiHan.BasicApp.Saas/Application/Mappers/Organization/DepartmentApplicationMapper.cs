#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentApplicationMapper
// Guid:e7085f26-0774-463a-b924-133f40e08eba
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 部门应用层映射器
/// </summary>
public static class DepartmentApplicationMapper
{
    /// <summary>
    /// 映射部门创建命令
    /// </summary>
    public static DepartmentCreateCommand ToCreateCommand(DepartmentCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DepartmentCreateCommand(
            input.ParentId,
            input.DepartmentName,
            input.DepartmentCode,
            input.DepartmentType,
            input.LeaderId,
            input.Phone,
            input.Email,
            input.Address,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射部门更新命令
    /// </summary>
    public static DepartmentUpdateCommand ToUpdateCommand(DepartmentUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DepartmentUpdateCommand(
            input.BasicId,
            input.ParentId,
            input.DepartmentName,
            input.DepartmentType,
            input.LeaderId,
            input.Phone,
            input.Email,
            input.Address,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射部门状态命令
    /// </summary>
    public static DepartmentStatusChangeCommand ToStatusCommand(DepartmentStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DepartmentStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射部门列表项
    /// </summary>
    /// <param name="department">部门实体</param>
    /// <returns>部门列表项 DTO</returns>
    public static DepartmentListItemDto ToListItemDto(SysDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentListItemDto
        {
            BasicId = department.BasicId,
            ParentId = department.ParentId,
            DepartmentName = department.DepartmentName,
            DepartmentCode = department.DepartmentCode,
            DepartmentType = department.DepartmentType,
            LeaderId = department.LeaderId,
            Phone = department.Phone,
            Email = department.Email,
            Status = department.Status,
            Sort = department.Sort,
            CreatedTime = department.CreatedTime,
            ModifiedTime = department.ModifiedTime
        };
    }

    /// <summary>
    /// 映射部门详情
    /// </summary>
    /// <param name="department">部门实体</param>
    /// <returns>部门详情 DTO</returns>
    public static DepartmentDetailDto ToDetailDto(SysDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentDetailDto
        {
            BasicId = department.BasicId,
            ParentId = department.ParentId,
            DepartmentName = department.DepartmentName,
            DepartmentCode = department.DepartmentCode,
            DepartmentType = department.DepartmentType,
            LeaderId = department.LeaderId,
            Phone = department.Phone,
            Email = department.Email,
            Address = department.Address,
            Status = department.Status,
            Sort = department.Sort,
            Remark = department.Remark,
            CreatedTime = department.CreatedTime,
            CreatedId = department.CreatedId,
            CreatedBy = department.CreatedBy,
            ModifiedTime = department.ModifiedTime,
            ModifiedId = department.ModifiedId,
            ModifiedBy = department.ModifiedBy
        };
    }

    /// <summary>
    /// 映射部门树节点
    /// </summary>
    /// <param name="department">部门实体</param>
    /// <returns>部门树节点 DTO</returns>
    public static DepartmentTreeNodeDto ToTreeNodeDto(SysDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentTreeNodeDto
        {
            BasicId = department.BasicId,
            ParentId = department.ParentId,
            DepartmentName = department.DepartmentName,
            DepartmentCode = department.DepartmentCode,
            DepartmentType = department.DepartmentType,
            LeaderId = department.LeaderId,
            Status = department.Status,
            Sort = department.Sort
        };
    }
}
