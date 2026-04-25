#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictItem
// Guid:4c28152c-d6e9-4396-addb-b479254bad28
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字典项实体
/// 字典的具体选项，支持父子结构（多级联动字典）
/// </summary>
/// <remarks>
/// 关联：
/// - DictId → SysDict；ParentId → SysDictItem（自关联，支持树形）
///
/// 写入：
/// - DictId + ItemCode 唯一（UX_DiId_ItCo），同字典下项编码不能重复
/// - 多级联动（如省-市-区）：通过 ParentId 自关联构建
/// - 同租户约束：DictId 必须与当前 TenantId 一致
///
/// 查询：
/// - 字典选项加载：WHERE DictId=? AND Status=Yes ORDER BY Sort
/// - 树形联动：IX_PaId + 递归
///
/// 删除：
/// - 仅软删；删除父项前必须处理子项
///
/// 状态：
/// - Status: Yes/No
///
/// 场景：
/// - 下拉框选项加载
/// - 多级联动选择（地区三级、组织架构选择）
/// </remarks>
[SugarTable("SysDictItem", "系统字典项表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_DiId_ItCo", nameof(TenantId), OrderByType.Asc, nameof(DictId), OrderByType.Asc, nameof(ItemCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_PaId", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_DiId", nameof(TenantId), OrderByType.Asc, nameof(DictId), OrderByType.Asc)]
public partial class SysDictItem : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 字典ID
    /// </summary>
    [SugarColumn(ColumnDescription = "字典ID", IsNullable = false)]
    public virtual long DictId { get; set; }

    /// <summary>
    /// 父级字典项ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父级字典项ID", IsNullable = true)]
    public virtual long? ParentId { get; set; }

    /// <summary>
    /// 字典项编码
    /// </summary>
    [SugarColumn(ColumnDescription = "字典项编码", Length = 100, IsNullable = false)]
    public virtual string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典项名称
    /// </summary>
    [SugarColumn(ColumnDescription = "字典项名称", Length = 100, IsNullable = false)]
    public virtual string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 字典项值
    /// </summary>
    [SugarColumn(ColumnDescription = "字典项值", Length = 200, IsNullable = true)]
    public virtual string? ItemValue { get; set; }

    /// <summary>
    /// 字典项描述
    /// </summary>
    [SugarColumn(ColumnDescription = "字典项描述", Length = 500, IsNullable = true)]
    public virtual string? ItemDescription { get; set; }

    /// <summary>
    /// 扩展元数据（JSON格式，替代固定扩展列，支持自描述的灵活扩展）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展元数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
