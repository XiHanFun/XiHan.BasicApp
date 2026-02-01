#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDict.pl
// Guid:3d28152c-d6e9-4396-addb-b479254bad37
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:52:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统字典实体扩展
/// </summary>
public partial class SysDict
{
    /// <summary>
    /// 字典项列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysDictItem.DictId))]
    public virtual List<SysDictItem>? DictItems { get; set; }
}
