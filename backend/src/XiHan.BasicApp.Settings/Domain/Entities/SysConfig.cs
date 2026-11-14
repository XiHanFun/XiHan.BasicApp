// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 系统参数（平台库实体，不实现 <c>IMultiTenant</c>）
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 为 <c>platform</c> 的是平台参数，否则是该租户的参数
/// 租户新建与平台参数同键的参数即覆盖它（平台参数 <see cref="AllowTenantOverride"/> 为 <c>false</c> 时不许）；生效值取租户的、再取平台的，停用的视为不存在
/// </remarks>
[SugarTable(TableName = "sys_config", TableDescription = "系统参数（平台库实体，不实现 IMultiTenant）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Key), Name = "ux_sys_config_owner_tenant_id_key")]
public sealed class SysConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 参数键
    /// </summary>
    /// <remarks>
    /// 点分小写（如 <c>auditing.retention-days</c>），所属一侧内唯一，创建后不可改
    /// </remarks>
    [SugarColumn(ColumnName = "key", Length = 512, IsNullable = false, ColumnDescription = "参数键")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分组（自由文本，用于列表归类）
    /// </summary>
    [SugarColumn(ColumnName = "group", Length = 512, IsNullable = true, ColumnDescription = "分组（自由文本，用于列表归类）")]
    public string? Group { get; set; }

    /// <summary>
    /// 参数值
    /// </summary>
    /// <remarks>
    /// <see cref="IsEncrypted"/> 时是数据保护的密文
    /// </remarks>
    [SugarColumn(ColumnName = "value", IsNullable = true, ColumnDescription = "参数值")]
    [LongText]
    public string? Value { get; set; }

    /// <summary>
    /// 缺省值：值为空时的生效值，不加密
    /// </summary>
    [SugarColumn(ColumnName = "default_value", IsNullable = true, ColumnDescription = "缺省值：值为空时的生效值，不加密")]
    [LongText]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    [SugarColumn(ColumnName = "data_type", IsNullable = false, ColumnDescription = "数据类型")]
    public ConfigDataType DataType { get; set; } = ConfigDataType.Text;

    /// <summary>
    /// 值是否加密落库（读出不回显）
    /// </summary>
    [SugarColumn(ColumnName = "is_encrypted", IsNullable = false, ColumnDescription = "值是否加密落库（读出不回显）")]
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 内建（种子写入）：不能删除、不能改数据类型
    /// </summary>
    [SugarColumn(ColumnName = "is_built_in", IsNullable = false, ColumnDescription = "内建（种子写入）：不能删除、不能改数据类型")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 平台参数是否允许租户同键覆盖
    /// </summary>
    /// <remarks>
    /// 租户参数恒为 <c>false</c>
    /// </remarks>
    [SugarColumn(ColumnName = "allow_tenant_override", IsNullable = false, ColumnDescription = "平台参数是否允许租户同键覆盖")]
    public bool AllowTenantOverride { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public SettingStatus Status { get; set; } = SettingStatus.Active;

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
