#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSessionRole.pl
// Guid:01234567-8901-2345-6789-890123456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/7 10:46:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统会话角色映射实体扩展
/// </summary>
public partial class SysSessionRole
{
    /// <summary>
    /// 会话
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(SessionId))]
    public virtual SysUserSession? Session { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public virtual SysRole? Role { get; set; }
}
