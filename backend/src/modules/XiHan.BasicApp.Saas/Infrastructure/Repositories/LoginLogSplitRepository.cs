#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogSplitRepository
// Guid:a1b2c3d4-0011-0004-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 登录日志分表仓储实现
/// </summary>
public class LoginLogSplitRepository(ISqlSugarClientResolver clientResolver, ISplitTableLocator locator)
    : SqlSugarSplitRepository<SysLoginLog>(clientResolver, locator), ILoginLogSplitRepository;
