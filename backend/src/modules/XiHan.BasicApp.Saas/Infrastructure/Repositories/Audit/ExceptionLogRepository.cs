#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExceptionLogRepository
// Guid:187ff7d4-50fc-44c2-b7c3-4c7557f4f3f0
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
/// 异常日志仓储实现
/// </summary>
public sealed class ExceptionLogRepository(
    ISqlSugarClientResolver clientResolver,
    ISplitTableLocator splitTableLocator)
    : SaasSplitRepository<SysExceptionLog>(clientResolver, splitTableLocator), IExceptionLogRepository;
