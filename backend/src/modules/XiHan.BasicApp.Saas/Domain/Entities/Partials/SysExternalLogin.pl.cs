#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysExternalLogin.pl
// Guid:b2c3d4e5-6f70-8192-a3b4-c5d6e7f80912
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统第三方登录绑定实体扩展
/// </summary>
public partial class SysExternalLogin
{
    /// <summary>
    /// 关联用户
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }
}
