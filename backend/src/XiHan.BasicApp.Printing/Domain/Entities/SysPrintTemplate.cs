// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Printing.Domain.Entities;

/// <summary>
/// 打印模板（平台库实体，不实现 <c>IMultiTenant</c>）
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 为 <c>platform</c> 的是平台全局模板，否则是该租户的私有模板
/// 独立库的租户也读写平台库里的这张表。编码在所属一侧唯一、创建后不可改；
/// 数据源编码可空（自由模板），只提供字段契约、设计素材与缺省样例。并发戳参与修改与启停的乐观并发
/// </remarks>
[SugarTable(TableName = "sys_print_template", TableDescription = "打印模板（平台库实体，不实现 IMultiTenant）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(TemplateCode), Name = "ux_sys_print_template_owner_tenant_id_template_code")]
public sealed class SysPrintTemplate : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 模板编码
    /// </summary>
    /// <remarks>
    /// 不含空白、不超过 100 个字符，所属一侧内唯一，创建后不可改
    /// </remarks>
    [SugarColumn(ColumnName = "template_code", Length = 512, IsNullable = false, ColumnDescription = "模板编码")]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 数据源编码
    /// </summary>
    /// <remarks>
    /// 为空即自由模板；写入时须已登记
    /// </remarks>
    [SugarColumn(ColumnName = "data_source_code", Length = 512, IsNullable = true, ColumnDescription = "数据源编码")]
    public string? DataSourceCode { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(ColumnName = "template_name", Length = 512, IsNullable = false, ColumnDescription = "模板名称")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 打印设计器的模板 JSON
    /// </summary>
    /// <remarks>
    /// 大文本、没有业务上限；后端只做最小结构校验（<c>schemaVersion</c> 为 1、<c>pages</c> 是非空的对象数组），其余原样保存
    /// </remarks>
    [SugarColumn(ColumnName = "template_json", IsNullable = false, ColumnDescription = "打印设计器的模板 JSON")]
    [LongText]
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>
    /// 全局模板是否向租户开放
    /// </summary>
    /// <remarks>
    /// 租户私有模板恒为 <c>false</c>
    /// </remarks>
    [SugarColumn(ColumnName = "allow_tenant_use", IsNullable = false, ColumnDescription = "全局模板是否向租户开放")]
    public bool AllowTenantUse { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public PrintTemplateStatus Status { get; set; } = PrintTemplateStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 512, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 打印模板的启停状态
/// </summary>
/// <remarks>
/// 停用的模板解析不到，只有停用的模板能删除
/// </remarks>
public enum PrintTemplateStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}
