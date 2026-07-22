// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 部门分页查询 DTO
/// </summary>
public sealed class DepartmentPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（部门编码、名称、电话、邮箱、地址、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 父级部门主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType? DepartmentType { get; set; }

    /// <summary>
    /// 负责人主键
    /// </summary>
    public long? LeaderId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
