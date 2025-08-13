#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthToken.pl
// Guid:7d28152c-d6e9-4396-addb-b479254bad41
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:56:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// OAuth令牌实体扩展
/// </summary>
public partial class SysOAuthToken
{
    /// <summary>
    /// OAuth应用信息
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ClientId), nameof(SysOAuthApp.ClientId))]
    public virtual SysOAuthApp? OAuthApp { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }
}
