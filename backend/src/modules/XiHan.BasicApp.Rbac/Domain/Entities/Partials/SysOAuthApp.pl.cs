#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthApp.pl
// Guid:5d28152c-d6e9-4396-addb-b479254bad39
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:54:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

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
