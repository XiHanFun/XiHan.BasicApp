#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentDetailDto
// Guid:b4c4c2f9-5786-4df4-8a07-1cc1c62f65d2
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
/// 用户部门归属详情 DTO
/// </summary>
public sealed class UserDepartmentDetailDto : BasicAppDto
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

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }
}
