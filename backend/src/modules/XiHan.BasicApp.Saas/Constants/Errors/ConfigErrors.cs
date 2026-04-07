#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigErrors
// Guid:de1cb3cf-c0eb-4cf4-d5a6-b12c3d4e5f6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Errors;

/// <summary>
/// 配置模块错误消息
/// </summary>
public static class ConfigErrors
{
    /// <summary>
    /// 配置不存在
    /// </summary>
    public const string NotFound = "配置不存在";

    /// <summary>
    /// 配置键已存在
    /// </summary>
    public const string KeyExists = "配置键已存在";

    /// <summary>
    /// 内置配置不可删除
    /// </summary>
    public const string BuiltInCannotDelete = "内置配置不可删除";

    /// <summary>
    /// 内置配置键不可修改
    /// </summary>
    public const string BuiltInKeyCannotChange = "内置配置的配置键不可修改";
}
