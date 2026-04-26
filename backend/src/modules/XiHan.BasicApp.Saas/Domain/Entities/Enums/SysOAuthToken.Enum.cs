#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthToken.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 授权类型枚举
/// </summary>
public enum GrantType
{
    /// <summary>
    /// 授权码模式
    /// </summary>
    AuthorizationCode = 0,

    /// <summary>
    /// 简化模式
    /// </summary>
    Implicit = 1,

    /// <summary>
    /// 密码模式
    /// </summary>
    Password = 2,

    /// <summary>
    /// 客户端凭证模式
    /// </summary>
    ClientCredentials = 3,

    /// <summary>
    /// 刷新令牌
    /// </summary>
    RefreshToken = 4
}

