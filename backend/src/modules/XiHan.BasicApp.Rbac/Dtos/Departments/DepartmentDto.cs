#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentDto
// Guid:6a2b3c4d-5e6f-7890-abcd-ef1234567895
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Dtos.Departments;

/// <summary>
/// 部门 DTO
/// </summary>
public class DepartmentDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 父级部门ID
    /// </summary>
    public RbacIdType? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType DepartmentType { get; set; }

    /// <summary>
    /// 负责人ID
    /// </summary>
    public RbacIdType? LeaderId { get; set; }

    /// <summary>
    /// 负责人姓名
    /// </summary>
    public string? LeaderName { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 部门树 DTO
/// </summary>
public class DepartmentTreeDto : DepartmentDto
{
    /// <summary>
    /// 子部门列表
    /// </summary>
    public List<DepartmentTreeDto> Children { get; set; } = [];
}

/// <summary>
/// 创建部门 DTO
/// </summary>
public class CreateDepartmentDto : RbacCreationDtoBase
{
    /// <summary>
    /// 父级部门ID
    /// </summary>
    public RbacIdType? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 负责人ID
    /// </summary>
    public RbacIdType? LeaderId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新部门 DTO
/// </summary>
public class UpdateDepartmentDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 父级部门ID
    /// </summary>
    public RbacIdType? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType? DepartmentType { get; set; }

    /// <summary>
    /// 负责人ID
    /// </summary>
    public RbacIdType? LeaderId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}