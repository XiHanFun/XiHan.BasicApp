#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResetUserPasswordCommand
// Guid:2e288e69-172f-4fe0-b7ce-5f18ab45a512
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/09 16:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 重置用户密码命令
/// </summary>
public class ResetUserPasswordCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "用户 ID 无效")]
    public long UserId { get; set; }

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "新密码不能为空")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
