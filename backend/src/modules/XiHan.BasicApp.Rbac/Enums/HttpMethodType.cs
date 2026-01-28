#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:HttpMethodType
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/7 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// HTTP 方法枚举
/// </summary>
public enum HttpMethodType
{
    /// <summary>
    /// GET 请求
    /// </summary>
    GET = 0,

    /// <summary>
    /// POST 请求
    /// </summary>
    POST = 1,

    /// <summary>
    /// PUT 请求
    /// </summary>
    PUT = 2,

    /// <summary>
    /// DELETE 请求
    /// </summary>
    DELETE = 3,

    /// <summary>
    /// PATCH 请求
    /// </summary>
    PATCH = 4,

    /// <summary>
    /// HEAD 请求
    /// </summary>
    HEAD = 5,

    /// <summary>
    /// OPTIONS 请求
    /// </summary>
    OPTIONS = 6,

    /// <summary>
    /// 所有方法
    /// </summary>
    ALL = 99
}
