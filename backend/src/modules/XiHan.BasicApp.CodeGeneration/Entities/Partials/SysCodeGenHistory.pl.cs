#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenHistory.pl
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567113
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 10:34:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.CodeGeneration.Entities;

/// <summary>
/// 系统代码生成历史记录实体扩展
/// </summary>
public partial class SysCodeGenHistory
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
    /// 所属表信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(TableId))]
    public virtual SysCodeGenTable? Table { get; set; }

    /// <summary>
    /// 操作用户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(OperatorId))]
    public virtual SysUser? Operator { get; set; }
}
