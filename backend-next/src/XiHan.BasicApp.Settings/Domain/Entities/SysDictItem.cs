// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 字典项（平台库实体，属主与所在字典相同）。
/// </summary>
/// <remarks>
/// 同一字典内编码唯一，可按 <see cref="ParentId"/> 组成树（上级在同一字典、不成环）；每个字典至多一个缺省项。
/// 业务数据按 <see cref="Code"/> 存；停用的项照常给出（标为停用，只用于回显历史数据）。
/// </remarks>
public sealed class SysDictItem : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 所在字典。
    /// </summary>
    public Guid DictId { get; set; }

    /// <summary>
    /// 上级项。
    /// </summary>
    /// <remarks>
    /// 顶层为 <c>null</c>。
    /// </remarks>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 项编码：字典内唯一，创建后不可改。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 附加值（如外部系统的取值）。
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 扩展元数据（JSON）。
    /// </summary>
    [LongText]
    public string? Metadata { get; set; }

    /// <summary>
    /// 是否为字典的缺省项。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    /// <summary>
    /// 排序号（同级内）。
    /// </summary>
    public int Order { get; set; }
}
