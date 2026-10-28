// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 系统参数（平台库实体，不实现 <c>IMultiTenant</c>）：<see cref="OwnerTenantId"/> 为 <c>platform</c> 的是平台参数，否则是该租户的参数。
/// 租户新建与平台参数同键的参数即覆盖它（平台参数 <see cref="AllowTenantOverride"/> 为 <c>false</c> 时不许）；生效值取租户的、再取平台的，停用的视为不存在。
/// </summary>
public sealed class SysConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 参数键：点分小写（如 <c>auditing.retention-days</c>），所属一侧内唯一，创建后不可改。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分组（自由文本，用于列表归类）。
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 参数值；<see cref="IsEncrypted"/> 时是数据保护的密文。
    /// </summary>
    [LongText]
    public string? Value { get; set; }

    /// <summary>
    /// 缺省值：值为空时的生效值，不加密。
    /// </summary>
    [LongText]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 数据类型。
    /// </summary>
    public ConfigDataType DataType { get; set; } = ConfigDataType.Text;

    /// <summary>
    /// 值是否加密落库（读出不回显）。
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 内建（种子写入）：不能删除、不能改数据类型。
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 平台参数是否允许租户同键覆盖；租户参数恒为 <c>false</c>。
    /// </summary>
    public bool AllowTenantOverride { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }
}
