#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRegisterCommand
// Guid:0f5d865b-6143-4823-a385-7b70609c26c1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/13 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 用户注册命令
/// </summary>
public class UserRegisterCommand
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在 3~50 之间")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(200, MinimumLength = 6, ErrorMessage = "密码长度必须在 6~200 之间")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [StringLength(50, ErrorMessage = "昵称长度不能超过 50")]
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过 100")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [StringLength(20, ErrorMessage = "手机号长度不能超过 20")]
    public string? Phone { get; set; }

    /// <summary>
    /// 目标租户ID
    /// </summary>
    public long? TargetTenantId { get; set; }

    /// <summary>
    /// 目标租户编码
    /// </summary>
    [StringLength(64, ErrorMessage = "租户编码长度不能超过 64")]
    public string? TargetTenantCode { get; set; }
}
