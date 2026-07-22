// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字典项实体扩展
/// </summary>
public partial class SysDictItem
{
    /// <summary>
    /// 所属字典
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(DictId))]
    public virtual SysDict? Dict { get; set; }

    /// <summary>
    /// 父级字典项（多子对一父，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(ParentId))]
    public virtual SysDictItem? ParentItem { get; set; }

    /// <summary>
    /// 子字典项列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(ParentId))]
    public virtual List<SysDictItem>? Children { get; set; }
}
