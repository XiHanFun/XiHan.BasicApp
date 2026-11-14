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
/// 平台参数要让每个租户都读得到，所以落平台库、不实现 <c>IMultiTenant</c>，用 <see cref="OwnerTenantId"/> 分平台参数（<c>platform</c>）与租户参数：读共享（平台的加本租户的），写只本侧
/// 租户可以建与平台参数同键的参数来覆盖它，生效值先取租户的、再取平台的，停用的视为不存在：平台定缺省、租户按需改，不用给每个租户复制一份；平台参数可用 <see cref="AllowTenantOverride"/> 禁止覆盖，删掉租户的那份即回到平台的值
/// 参数键在本侧唯一、创建后不可改，各包按键读；唯一是部分唯一索引，软删除后同键可重建
/// 内建参数由各包经 <c>AddConfigCatalog</c> 登记（<see cref="ConfigDescriptor"/>）、种子 <c>settings.configs</c> 对齐：名称、数据类型、缺省值、加密与否随代码走，值、状态与排序号只首写、之后归运营
/// 内建的不能删、不能改数据类型：类型由登记它的包决定，种子发现现值不符登记的类型即报错，要由人改对
/// 需要保密的值加密落库、读出不回显，缺省值不加密；关掉加密而没给新值即清空值，否则解密落库等于让只有改权限的人在响应与差异日志里读到明文
/// 读经 <see cref="IConfigValueReader"/> 走读穿缓存（<c>ConfigEntries</c>）：按（侧、键）各缓存一份，平台参数改了只失效平台那一份、不用逐个租户失效；加密的存密文，明文不进缓存
/// 写入后的失效经 <c>CacheInvalidation</c> 推到工作单元结束之后再删：提交前的旧数据不会被并发的读回填进缓存
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
