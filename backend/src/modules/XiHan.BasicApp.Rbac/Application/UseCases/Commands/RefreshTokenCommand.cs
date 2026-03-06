#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RefreshTokenCommand
// Guid:4d7f4e8d-1d14-4f88-9f8a-a7f8bf5db5d1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Rbac.Application.UseCases.Commands;

/// <summary>
/// 刷新令牌命令
/// </summary>
public class RefreshTokenCommand
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "刷新令牌格式不正确")]
    public string RefreshToken { get; set; } = string.Empty;
}
