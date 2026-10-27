// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// <c>profile.change-password</c> 的请求：本人改密，须给当前密码。
/// </summary>
/// <param name="CurrentPassword">当前密码。</param>
/// <param name="NewPassword">新密码（策略：最短 8 位、含字母与数字、不与最近的历史密码相同）。</param>
public sealed record ChangePasswordRequest(
    [Required, StringLength(128, MinimumLength = 1)] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 8)] string NewPassword);
