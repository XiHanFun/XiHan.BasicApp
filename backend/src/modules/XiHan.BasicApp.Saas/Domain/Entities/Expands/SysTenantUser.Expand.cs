#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantUser.Expand
// Guid:e2f3a4b5-c6d7-8901-ef01-567890123456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统租户成员实体扩展
/// </summary>
public partial class SysTenantUser
{
    /// <summary>
    /// 用户
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }
}
