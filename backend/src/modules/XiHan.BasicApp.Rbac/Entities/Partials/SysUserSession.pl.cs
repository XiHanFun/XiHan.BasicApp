#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSession.pl
// Guid:23456789-0123-4567-8901-012345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 11:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户会话实体扩展
/// </summary>
public partial class SysUserSession
{
    /// <summary>
    /// 用户
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }

    /// <summary>
    /// 会话角色映射列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysSessionRole.SessionId))]
    public virtual List<SysSessionRole>? SessionRoles { get; set; }
}
