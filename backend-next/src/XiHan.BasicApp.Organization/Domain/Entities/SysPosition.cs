// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 岗位（租户库实体）。
/// </summary>
/// <remarks>
/// 归属记录引用它；被引用时不能删除。
/// </remarks>
public sealed class SysPosition : TenantScopedAuditedEntity
{
    /// <summary>
    /// 岗位编码：租户内唯一，字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 状态。
    /// </summary>
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }
}
