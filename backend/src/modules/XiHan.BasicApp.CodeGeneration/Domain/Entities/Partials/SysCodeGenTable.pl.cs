#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTable.pl
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567110
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 10:32:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;

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
    [Navigate(NavigateType.OneToMany, nameof(SysCodeGenTable.MasterTableId))]
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
