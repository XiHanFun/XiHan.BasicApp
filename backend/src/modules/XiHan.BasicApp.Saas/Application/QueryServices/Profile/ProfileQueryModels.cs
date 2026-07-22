// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 当前用户安全上下文
/// </summary>
public sealed record ProfileUserSecurityContext(SysUser User, SysUserSecurity Security);
