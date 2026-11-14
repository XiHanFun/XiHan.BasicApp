// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 字典（平台库实体）
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 分平台字典与租户字典。字典不覆盖：平台建字典要求全库没有同码，租户建字典要求平台与本租户都没有同码
/// 选项由 <see cref="SysDictItem"/> 承载；停用的字典不再给出选项
/// </remarks>
[SugarTable(TableName = "sys_dict", TableDescription = "字典（平台库实体）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_dict_owner_tenant_id_code")]
public sealed class SysDict : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 字典编码：字母开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 100，创建后不可改
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "字典编码：字母开头，只含字母、数字、.、_、-，最长 100，创建后不可改")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类（自由文本，用于列表归类）
    /// </summary>
    [SugarColumn(ColumnName = "category", Length = 512, IsNullable = true, ColumnDescription = "分类（自由文本，用于列表归类）")]
    public string? Category { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 内建（种子写入）：不能删除
    /// </summary>
    [SugarColumn(ColumnName = "is_built_in", IsNullable = false, ColumnDescription = "内建（种子写入）：不能删除")]
    public bool IsBuiltIn { get; set; }

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
}
