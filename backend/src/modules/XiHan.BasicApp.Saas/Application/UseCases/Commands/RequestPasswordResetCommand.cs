#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RequestPasswordResetCommand
// Guid:2fd0812f-90b8-47f7-9b2f-c4994fef7e48
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/13 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 申请重置密码命令
/// </summary>
public class RequestPasswordResetCommand
{
    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过 100")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 目标租户ID
    /// </summary>
    public long? TargetTenantId { get; set; }
}
