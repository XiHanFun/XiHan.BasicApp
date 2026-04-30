#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentListItemDto
// Guid:2b3e6d25-88ab-4c3b-9498-a2bf7d0b8b89
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户部门归属列表项 DTO
/// </summary>
public sealed class UserDepartmentListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 部门主键
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 部门编码
    /// </summary>
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 父级部门主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType? DepartmentType { get; set; }

    /// <summary>
    /// 部门状态
    /// </summary>
    public EnableStatus? DepartmentStatus { get; set; }

    /// <summary>
    /// 是否主部门
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// 归属状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
