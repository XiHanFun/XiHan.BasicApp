#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurity.pl
// Guid:7bd056f9-0739-4921-a278-25f404c8ea63
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:06:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 系统用户安全状态实体扩展
/// </summary>
public partial class SysUserSecurity
{
    /// <summary>
    /// 用户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }
}
