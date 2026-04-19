#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFieldLevelSecurity.pl
// Guid:d1e2f3a4-b5c6-7890-def0-456789012345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字段级安全实体扩展
/// </summary>
public partial class SysFieldLevelSecurity
{
    /// <summary>
    /// 关联资源
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(ResourceId))]
    public virtual SysResource? Resource { get; set; }
}
