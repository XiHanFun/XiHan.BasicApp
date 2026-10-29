// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.reset-password</c> 的请求。
/// </summary>
/// <remarks>
/// 管理员给用户设新密码，用户下次登录必须改密。标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">用户 ID。</param>
/// <param name="NewPassword">新密码（策略：最短 8 位、含字母与数字、不与最近的历史密码相同）。</param>
public sealed record ResetPasswordRequest(Guid Id, [Required, StringLength(128, MinimumLength = 8)] string NewPassword);
