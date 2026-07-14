#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionLockDtos
// Guid:8b2e5c41-6a97-4d30-b58f-0c4a19d7e263
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/15 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 锁屏请求
/// </summary>
public sealed class LockSessionRequestDto
{
    /// <summary>
    /// 锁屏密码（会话级一次性口令，解锁时校验；服务端不接受空值）
    /// </summary>
    [Required(ErrorMessage = "锁屏密码不能为空")]
    [StringLength(64, MinimumLength = 4, ErrorMessage = "锁屏密码长度须为 4-64 位")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 解锁请求
/// </summary>
public sealed class UnlockSessionRequestDto
{
    /// <summary>
    /// 锁屏密码
    /// </summary>
    [Required(ErrorMessage = "锁屏密码不能为空")]
    public string Password { get; set; } = string.Empty;
}
