#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictItem
// Guid:4c28152c-d6e9-4396-addb-b479254bad28
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统字典项实体
/// </summary>
[SugarTable("sys_dict_item", "系统字典项表")]
[SugarIndex("IX_SysDictItem_DictId", "DictId", OrderByType.Asc)]
[SugarIndex("IX_SysDictItem_ItemCode", "ItemCode", OrderByType.Asc)]
public partial class SysDictItem : SugarEntityWithAudit<long>
{
    /// <summary>
    /// 字典ID
    /// </summary>
    [SugarColumn(ColumnDescription = "字典ID", IsNullable = false)]
    public virtual long DictId { get; set; }

    /// <summary>
    /// 字典编码（冗余字段，便于查询）
    /// </summary>
    [SugarColumn(ColumnDescription = "字典编码", Length = 100, IsNullable = false)]
    public virtual string DictCode { get; set; } = string.Empty;

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
    /// 扩展属性1
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展属性1", Length = 200, IsNullable = true)]
    public virtual string? ExtendField1 { get; set; }

    /// <summary>
    /// 扩展属性2
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展属性2", Length = 200, IsNullable = true)]
    public virtual string? ExtendField2 { get; set; }

    /// <summary>
    /// 扩展属性3
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展属性3", Length = 200, IsNullable = true)]
    public virtual string? ExtendField3 { get; set; }

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
