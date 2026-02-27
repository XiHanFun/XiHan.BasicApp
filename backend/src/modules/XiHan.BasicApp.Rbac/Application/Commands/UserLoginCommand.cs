#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserLoginCommand
// Guid:164ec453-5ede-41f1-858d-2245f1814d06
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:44:10
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 用户登录命令
/// </summary>
public class UserLoginCommand
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "用户名长度必须在 1～50 之间")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 登录IP
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }
}
