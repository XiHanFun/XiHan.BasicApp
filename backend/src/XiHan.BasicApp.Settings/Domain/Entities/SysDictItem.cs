// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 字典项（平台库实体，属主与所在字典相同）
/// </summary>
/// <remarks>
/// 同一字典内编码唯一，可按 <see cref="ParentId"/> 组成树（上级在同一字典、不成环）；每个字典至多一个缺省项
/// 业务数据按 <see cref="Code"/> 存；停用的项照常给出（标为停用，只用于回显历史数据）
/// </remarks>
[SugarTable(TableName = "sys_dict_item", TableDescription = "字典项（平台库实体，属主与所在字典相同）")]
[ActiveUniqueIndex(nameof(DictId), nameof(Code), Name = "ux_sys_dict_item_dict_id_code")]
[SugarIndex("ix_sys_dict_item_parent_id", nameof(ParentId), OrderByType.Asc)]
public sealed class SysDictItem : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 所在字典
    /// </summary>
    [SugarColumn(ColumnName = "dict_id", IsNullable = false, ColumnDescription = "所在字典")]
    public Guid DictId { get; set; }

    /// <summary>
    /// 上级项
    /// </summary>
    /// <remarks>
    /// 顶层为 <c>null</c>
    /// </remarks>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDescription = "上级项")]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 项编码：字典内唯一，创建后不可改
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "项编码：字典内唯一，创建后不可改")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "显示名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 附加值（如外部系统的取值）
    /// </summary>
    [SugarColumn(ColumnName = "value", Length = 512, IsNullable = true, ColumnDescription = "附加值（如外部系统的取值）")]
    public string? Value { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 扩展元数据（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "metadata", IsNullable = true, ColumnDescription = "扩展元数据（JSON）")]
    [LongText]
    public string? Metadata { get; set; }

    /// <summary>
    /// 是否为字典的缺省项
    /// </summary>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否为字典的缺省项")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    /// <summary>
    /// 排序号（同级内）
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号（同级内）")]
    public int Order { get; set; }
}
