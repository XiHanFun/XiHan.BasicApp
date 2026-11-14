// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 岗位（租户库实体）
/// </summary>
/// <remarks>
/// 归属记录引用它；被引用时不能删除
/// </remarks>
[SugarTable(TableName = "sys_position", TableDescription = "岗位（租户库实体）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(Code), Name = "ux_sys_position_tenant_id_code")]
public sealed class SysPosition : TenantScopedAuditedEntity
{
    /// <summary>
    /// 岗位编码：租户内唯一，字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "岗位编码：租户内唯一，字母或数字开头，只含字母、数字、.、_、-，最长 64")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }
}
