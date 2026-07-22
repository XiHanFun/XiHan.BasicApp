// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户数据范围列表项 DTO
/// </summary>
public sealed class UserDataScopeListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long? TenantMemberId { get; set; }

    /// <summary>
    /// 租户内显示名
    /// </summary>
    public string? TenantMemberDisplayName { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType? TenantMemberType { get; set; }

    /// <summary>
    /// 成员邀请状态
    /// </summary>
    public TenantMemberInviteStatus? TenantMemberInviteStatus { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public ValidityStatus? TenantMemberStatus { get; set; }

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
    /// 是否包含子部门
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// 状态
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
