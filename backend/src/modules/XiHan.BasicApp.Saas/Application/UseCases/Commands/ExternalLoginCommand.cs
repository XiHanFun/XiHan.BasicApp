#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginCommand
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567820
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 第三方登录命令（处理回调后的用户信息）
/// </summary>
public class ExternalLoginCommand
{
    /// <summary>
    /// 提供商名称
    /// </summary>
    [Required(ErrorMessage = "提供商名称不能为空")]
    [StringLength(50, MinimumLength = 1)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 提供商用户唯一标识
    /// </summary>
    [Required(ErrorMessage = "提供商用户标识不能为空")]
    [StringLength(200, MinimumLength = 1)]
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 目标租户ID
    /// </summary>
    public long? TargetTenantId { get; set; }

    /// <summary>
    /// 目标租户编码
    /// </summary>
    [StringLength(64)]
    public string? TargetTenantCode { get; set; }
}
