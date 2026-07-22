// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
