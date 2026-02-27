#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTemplate.pl
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567112
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 10:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 系统代码生成模板实体扩展
/// </summary>
public partial class SysCodeGenTemplate
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }
}
