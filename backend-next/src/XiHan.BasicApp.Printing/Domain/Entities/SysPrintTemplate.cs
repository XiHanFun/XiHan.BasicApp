// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Printing.Domain.Entities;

/// <summary>
/// 打印模板（平台库实体，不实现 <c>IMultiTenant</c>）。
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 为 <c>platform</c> 的是平台全局模板，否则是该租户的私有模板（4.x 以 <c>TenantId = 0</c> 表示全局）。
/// 独立库的租户也读写平台库里的这张表，与 4.x 的 <c>[PlatformDataSource]</c> 同。编码在所属一侧唯一、创建后不可改；
/// 数据源编码可空（自由模板），只提供字段契约、设计素材与缺省样例。并发戳参与修改与启停的乐观并发（4.x 的行版本）。
/// </remarks>
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(TemplateCode))]
public sealed class SysPrintTemplate : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 模板编码。
    /// </summary>
    /// <remarks>
    /// 不含空白、不超过 100 个字符，所属一侧内唯一，创建后不可改。
    /// </remarks>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 数据源编码。
    /// </summary>
    /// <remarks>
    /// 为空即自由模板；写入时须已登记。
    /// </remarks>
    public string? DataSourceCode { get; set; }

    /// <summary>
    /// 模板名称。
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 打印设计器的模板 JSON。
    /// </summary>
    /// <remarks>
    /// 大文本、没有业务上限；后端只做最小结构校验（<c>schemaVersion</c> 为 1、<c>pages</c> 是非空的对象数组），其余原样保存。
    /// </remarks>
    [LongText]
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>
    /// 全局模板是否向租户开放。
    /// </summary>
    /// <remarks>
    /// 租户私有模板恒为 <c>false</c>。
    /// </remarks>
    public bool AllowTenantUse { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public PrintTemplateStatus Status { get; set; } = PrintTemplateStatus.Active;

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }
}
