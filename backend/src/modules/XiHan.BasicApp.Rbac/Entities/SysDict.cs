#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDict
// Guid:3c28152c-d6e9-4396-addb-b479254bad27
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统字典实体
/// </summary>
[SugarTable("sys_dict", "系统字典表")]
[SugarIndex("IX_SysDict_DictCode", "DictCode", OrderByType.Asc, true)]
[SugarIndex("IX_SysDict_DictType", "DictType", OrderByType.Asc)]
public partial class SysDict : SugarEntityWithAudit<long>
{
    /// <summary>
    /// 字典编码
    /// </summary>
    [SugarColumn(ColumnDescription = "字典编码", Length = 100, IsNullable = false)]
    public virtual string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称
    /// </summary>
    [SugarColumn(ColumnDescription = "字典名称", Length = 100, IsNullable = false)]
    public virtual string DictName { get; set; } = string.Empty;

    /// <summary>
    /// 字典类型
    /// </summary>
    [SugarColumn(ColumnDescription = "字典类型", Length = 50, IsNullable = false)]
    public virtual string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 字典描述
    /// </summary>
    [SugarColumn(ColumnDescription = "字典描述", Length = 500, IsNullable = true)]
    public virtual string? DictDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnDescription = "是否内置")]
    public virtual bool IsBuiltIn { get; set; } = false;

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
