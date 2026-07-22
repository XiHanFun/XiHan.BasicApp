// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

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
    /// 会话角色映射列表（DSD：本会话中激活的角色）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysSessionRole.SessionId))]
    public virtual List<SysSessionRole>? SessionRoles { get; set; }

    /// <summary>
    /// 本会话下的 OAuth 令牌列表（Token 生命周期由 SysOAuthToken 负责）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOAuthToken.SessionId))]
    public virtual List<SysOAuthToken>? OAuthTokens { get; set; }
}
