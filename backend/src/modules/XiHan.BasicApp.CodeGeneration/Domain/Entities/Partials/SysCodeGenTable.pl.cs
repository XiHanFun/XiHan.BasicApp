// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 系统代码生成表配置实体扩展
/// </summary>
public partial class SysCodeGenTable
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <summary>
    /// 父菜单信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(ParentMenuId))]
    public virtual SysMenu? ParentMenu { get; set; }

    /// <summary>
    /// 主表信息（主子表场景）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(MasterTableId))]
    public virtual SysCodeGenTable? MasterTable { get; set; }

    /// <summary>
    /// 子表列表（主子表场景）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(MasterTableId))]
    public virtual List<SysCodeGenTable>? ChildTables { get; set; }

    /// <summary>
    /// 表字段配置列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysCodeGenTableColumn.TableId))]
    public virtual List<SysCodeGenTableColumn>? Columns { get; set; }

    /// <summary>
    /// 生成历史记录列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysCodeGenHistory.TableId))]
    public virtual List<SysCodeGenHistory>? Histories { get; set; }
}
