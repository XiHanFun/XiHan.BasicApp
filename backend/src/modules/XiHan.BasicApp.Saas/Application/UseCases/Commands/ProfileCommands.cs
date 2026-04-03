#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileCommands
// Guid:a7c3e4f5-8b2d-4a91-b6e0-1f2d3c4e5a6b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 更新个人资料命令
/// </summary>
public class UpdateProfileCommand
{
    /// <summary>
    /// 昵称
    /// </summary>
    [StringLength(50, ErrorMessage = "昵称长度不能超过 50")]
    public string? NickName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过 50")]
    public string? RealName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [StringLength(500, ErrorMessage = "头像地址长度不能超过 500")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过 100")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过 20")]
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender? Gender { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    public DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    [StringLength(50, ErrorMessage = "时区长度不能超过 50")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    [StringLength(10, ErrorMessage = "语言编码长度不能超过 10")]
    public string? Language { get; set; }

    /// <summary>
    /// 国家/地区
    /// </summary>
    [StringLength(50, ErrorMessage = "国家/地区长度不能超过 50")]
    public string? Country { get; set; }

    /// <summary>
    /// 个人简介
    /// </summary>
    [StringLength(500, ErrorMessage = "个人简介长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 撤销指定会话命令
/// </summary>
public class RevokeSessionCommand
{
    /// <summary>
    /// 会话ID
    /// </summary>
    [Required(ErrorMessage = "会话ID不能为空")]
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// 启用双因素认证命令（需传入验证码确认）
/// </summary>
public class Enable2FACommand
{
    /// <summary>
    /// 认证方式（1=TOTP 2=Email 3=Phone）
    /// </summary>
    [Required(ErrorMessage = "认证方式不能为空")]
    public TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(10, MinimumLength = 4, ErrorMessage = "验证码长度必须在 4～10 之间")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 禁用双因素认证命令（需传入验证码确认）
/// </summary>
public class Disable2FACommand
{
    /// <summary>
    /// 认证方式（与当前启用方式一致）
    /// </summary>
    [Required(ErrorMessage = "认证方式不能为空")]
    public TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(10, MinimumLength = 4, ErrorMessage = "验证码长度必须在 4～10 之间")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 发送 2FA 设置验证码命令（邮箱/手机方式启用或禁用时使用）
/// </summary>
public class Send2FAVerifyCodeCommand
{
    /// <summary>
    /// 目标方式（2=Email 3=Phone）
    /// </summary>
    [Required(ErrorMessage = "认证方式不能为空")]
    public TwoFactorMethod Method { get; set; }
}

/// <summary>
/// 修改用户名命令
/// </summary>
public class ChangeUserNameCommand
{
    /// <summary>
    /// 新用户名（3~30 字符，字母/数字/下划线，不能以数字开头）
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "用户名长度必须在 3～30 之间")]
    [RegularExpression(@"^[a-zA-Z_][a-zA-Z0-9_]{2,29}$", ErrorMessage = "用户名只能包含字母、数字、下划线，且不能以数字开头")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 当前密码（身份验证）
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 解除第三方账号绑定命令
/// </summary>
public class UnlinkExternalLoginCommand
{
    /// <summary>
    /// 提供商名称（google、github 等）
    /// </summary>
    [Required(ErrorMessage = "提供商不能为空")]
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// 验证邮箱命令
/// </summary>
public class VerifyEmailCommand
{
    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码必须为 6 位")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 停用账号命令
/// </summary>
public class DeactivateAccountCommand
{
    /// <summary>
    /// 当前密码（二次确认）
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 注销账号命令
/// </summary>
public class DeleteAccountCommand
{
    /// <summary>
    /// 当前密码（二次确认）
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}
