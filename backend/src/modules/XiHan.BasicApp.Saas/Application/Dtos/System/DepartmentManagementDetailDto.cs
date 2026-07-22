// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 部门管理详情聚合 DTO
/// </summary>
public sealed class DepartmentManagementDetailDto
{
    /// <summary>
    /// 部门基础详情
    /// </summary>
    public DepartmentDetailDto Department { get; set; } = new();

    /// <summary>
    /// 直属子部门
    /// </summary>
    public List<DepartmentListItemDto> ChildDepartments { get; set; } = [];

    /// <summary>
    /// 部门成员（不含子部门）
    /// </summary>
    public List<DepartmentManagementMemberDto> Members { get; set; } = [];

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedTime { get; set; }
}

/// <summary>
/// 部门管理成员项 DTO
/// </summary>
public sealed class DepartmentManagementMemberDto
{
    /// <summary>
    /// 用户部门归属主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 岗位主键
    /// </summary>
    public long? PositionId { get; set; }

    /// <summary>
    /// 岗位名称
    /// </summary>
    public string? PositionName { get; set; }

    /// <summary>
    /// 工号
    /// </summary>
    public string? JobNumber { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    public string? JobLevel { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    public DateTimeOffset? JoinTime { get; set; }

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
