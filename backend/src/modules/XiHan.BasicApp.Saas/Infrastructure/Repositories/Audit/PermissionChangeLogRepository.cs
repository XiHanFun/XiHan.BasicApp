#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionChangeLogRepository
// Guid:dcf8992e-5201-441f-865d-129876667d03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.SplitTables;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限变更日志仓储实现
/// </summary>
public sealed class PermissionChangeLogRepository(
    ISqlSugarClientResolver clientResolver,
    ISplitTableLocator splitTableLocator)
    : SaasSplitRepository<SysPermissionChangeLog>(clientResolver, splitTableLocator), IPermissionChangeLogRepository;
