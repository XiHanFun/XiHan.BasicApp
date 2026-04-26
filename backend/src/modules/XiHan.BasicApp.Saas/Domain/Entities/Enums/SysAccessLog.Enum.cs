#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAccessLog.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 访问结果枚举
/// </summary>
public enum AccessResult
{
    /// <summary>
    /// 成功
    /// </summary>
    [Description("成功")]
    Success = 0,

    /// <summary>
    /// 失败
    /// </summary>
    [Description("失败")]
    Failed = 1,

    /// <summary>
    /// 权限不足
    /// </summary>
    [Description("权限不足")]
    Forbidden = 2,

    /// <summary>
    /// 未授权
    /// </summary>
    [Description("未授权")]
    Unauthorized = 3,

    /// <summary>
    /// 资源不存在
    /// </summary>
    [Description("资源不存在")]
    NotFound = 4,

    /// <summary>
    /// 服务器错误
    /// </summary>
    [Description("服务器错误")]
    ServerError = 5
}
