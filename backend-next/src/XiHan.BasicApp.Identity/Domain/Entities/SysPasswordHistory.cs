// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 密码历史（平台库，表 <c>sys_password_history</c>，只增）：每次设置密码追加一行，改密时禁止与最近 N 个相同。
/// </summary>
public sealed class SysPasswordHistory : AppRecord
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 当时的密码散列。
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
}
