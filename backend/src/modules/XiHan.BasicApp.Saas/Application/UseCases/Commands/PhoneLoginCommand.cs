#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PhoneLoginCommand
// Guid:93966373-9b4f-4890-9fe6-4fce8436174b
// Author:zhaifanhua
// CreateTime:2026/03/13 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 手机验证码登录命令
/// </summary>
public class PhoneLoginCommand
{
    /// <summary>
    /// 手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "手机号格式不正确")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码长度必须为 6 位")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; } = 1;
}
