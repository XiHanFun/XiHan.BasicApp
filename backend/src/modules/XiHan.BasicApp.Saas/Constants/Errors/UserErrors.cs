#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserErrors
// Guid:ef2dc4da-d1fc-4da5-e6b7-c23d4e5f6a7b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Errors;

/// <summary>
/// 用户模块错误消息
/// </summary>
public static class UserErrors
{
    /// <summary>
    /// 用户不存在
    /// </summary>
    public const string NotFound = "用户不存在";

    /// <summary>
    /// 用户名已存在
    /// </summary>
    public const string NameExists = "用户名已存在";

    /// <summary>
    /// 邮箱已存在
    /// </summary>
    public const string EmailExists = "邮箱已存在";

    /// <summary>
    /// 手机号已存在
    /// </summary>
    public const string PhoneExists = "手机号已存在";

    /// <summary>
    /// 原密码错误
    /// </summary>
    public const string OldPasswordError = "原密码错误";

    /// <summary>
    /// 两次密码不一致
    /// </summary>
    public const string PasswordNotMatch = "两次密码不一致";
}
