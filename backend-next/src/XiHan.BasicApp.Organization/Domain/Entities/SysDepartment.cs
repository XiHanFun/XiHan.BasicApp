// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门（租户库实体，严格按租户过滤）。层级用 <see cref="ParentId"/> 表示直接上级，祖先 / 子孙关系由闭包表 <see cref="SysDepartmentHierarchy"/> 维护（数据范围「本部门及子部门」用它联查）。
/// </summary>
public sealed class SysDepartment : TenantScopedAuditedEntity
{
    /// <summary>
    /// 部门编码：租户内唯一，字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 直接上级部门；为空即顶层。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 负责人（本租户的用户）；为空即未指定。
    /// </summary>
    public Guid? LeaderUserId { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// 同级排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }
}
