// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统 OAuth 应用实体扩展
/// </summary>
public partial class SysOAuthApp
{
    /// <summary>
    /// 授权码列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOAuthCode.ClientId), nameof(ClientId))]
    public virtual List<SysOAuthCode>? AuthorizationCodes { get; set; }

    /// <summary>
    /// 令牌列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOAuthToken.ClientId), nameof(ClientId))]
    public virtual List<SysOAuthToken>? Tokens { get; set; }
}
