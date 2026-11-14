// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 岗位（租户库实体）
/// </summary>
/// <remarks>
/// 岗位是与部门正交的维度，挂在任职（<see cref="SysUserDepartment.PositionId"/>）上而不属于某个部门：同一个人在不同部门可以任不同岗位，同一岗位可以跨部门
/// 落租户库、只在租户侧管理，每个租户维护自己的岗位体系
/// 编码租户内唯一、建后不改，用部分唯一索引：软删除后同编码可以重建；流程按岗位找办理人、权限条件按岗位判定都按编码引用，编码要稳定
/// 软删除，还被任职引用的不能删，免得任职悬空；不用的岗位改停用，停用后不再出现在组织目录的选项与任职人里，也不进权限条件的岗位属性
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
