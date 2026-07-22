// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    [Description("授权码")]
    AuthorizationCode = 0,

    /// <summary>
    /// 简化模式
    /// </summary>
    [Description("简化")]
    Implicit = 1,

    /// <summary>
    /// 密码模式
    /// </summary>
    [Description("密码")]
    Password = 2,

    /// <summary>
    /// 客户端凭证模式
    /// </summary>
    [Description("客户端凭证")]
    ClientCredentials = 3,

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Description("刷新令牌")]
    RefreshToken = 4
}
