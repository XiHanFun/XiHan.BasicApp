// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门（租户库实体，严格按租户过滤）
/// </summary>
/// <remarks>
/// 层级用 <see cref="ParentId"/> 表示直接上级，祖先 / 子孙关系由闭包表 <see cref="SysDepartmentHierarchy"/> 维护（数据范围「本部门及子部门」用它联查）
/// </remarks>
[ActiveUniqueIndex(nameof(TenantId), nameof(Code))]
public sealed class SysDepartment : TenantScopedAuditedEntity
{
    /// <summary>
    /// 部门编码：租户内唯一，字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    public DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 直接上级部门
    /// </summary>
    /// <remarks>
    /// 为空即顶层
    /// </remarks>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 负责人（本租户的用户）
    /// </summary>
    /// <remarks>
    /// 为空即未指定
    /// </remarks>
    public Guid? LeaderUserId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// 同级排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 部门类型
/// </summary>
/// <remarks>
/// 部门是缺省值、排在 0，加列时已有的行读出来就是它（报文按名称，数值只在库里）
/// </remarks>
public enum DepartmentType
{
    /// <summary>
    /// 部门
    /// </summary>
    Department = 0,

    /// <summary>
    /// 集团
    /// </summary>
    Corporation = 1,

    /// <summary>
    /// 总部
    /// </summary>
    Headquarters = 2,

    /// <summary>
    /// 公司
    /// </summary>
    Company = 3,

    /// <summary>
    /// 分公司
    /// </summary>
    Branch = 4,

    /// <summary>
    /// 事业部
    /// </summary>
    Division = 5,

    /// <summary>
    /// 中心
    /// </summary>
    Center = 6,

    /// <summary>
    /// 科室
    /// </summary>
    Section = 7,

    /// <summary>
    /// 团队
    /// </summary>
    Team = 8,

    /// <summary>
    /// 小组
    /// </summary>
    Group = 9,

    /// <summary>
    /// 项目组
    /// </summary>
    Project = 10,

    /// <summary>
    /// 工作组
    /// </summary>
    Workgroup = 11,

    /// <summary>
    /// 虚拟组织
    /// </summary>
    Virtual = 12,

    /// <summary>
    /// 办事处
    /// </summary>
    Office = 13,

    /// <summary>
    /// 子公司
    /// </summary>
    Subsidiary = 14,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99,
}
