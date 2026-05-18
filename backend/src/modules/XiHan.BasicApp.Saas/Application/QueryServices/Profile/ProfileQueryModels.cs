#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileQueryModels
// Guid:014cbfd1-3c4e-4576-88a8-79b88e86dcab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 当前用户安全上下文
/// </summary>
public sealed record ProfileUserSecurityContext(SysUser User, SysUserSecurity Security);
