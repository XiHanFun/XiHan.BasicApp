#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentDto
// Guid:89ace6ff-2adb-40e2-baee-7fca22a2f40a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:43:17
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 部门 DTO
/// </summary>
public class DepartmentDto : BasicAppDto
{
    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

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
    public long? LeaderId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 创建部门 DTO
/// </summary>
public class DepartmentCreateDto : BasicAppCDto
{
    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [Required(ErrorMessage = "部门名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "部门名称长度必须在 1～100 之间")]
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    [Required(ErrorMessage = "部门编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "部门编码长度必须在 1～100 之间")]
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 负责人ID
    /// </summary>
    public long? LeaderId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [StringLength(20, ErrorMessage = "联系电话长度不能超过 20")]
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [StringLength(100, ErrorMessage = "邮箱长度不能超过 100")]
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [StringLength(500, ErrorMessage = "地址长度不能超过 500")]
    public string? Address { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新部门 DTO
/// </summary>
public class DepartmentUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [Required(ErrorMessage = "部门名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "部门名称长度必须在 1～100 之间")]
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    [Required(ErrorMessage = "部门编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "部门编码长度必须在 1～100 之间")]
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 负责人ID
    /// </summary>
    public long? LeaderId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [StringLength(20, ErrorMessage = "联系电话长度不能超过 20")]
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [StringLength(100, ErrorMessage = "邮箱长度不能超过 100")]
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [StringLength(500, ErrorMessage = "地址长度不能超过 500")]
    public string? Address { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
