#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityRepository
// Guid:29190661-d08a-4765-b5ba-94723d2d1523
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户安全状态仓储实现
/// </summary>
public sealed class UserSecurityRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserSecurity>(clientResolver), IUserSecurityRepository;
