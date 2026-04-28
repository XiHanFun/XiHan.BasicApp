#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogRepository
// Guid:2a27ef9e-88ec-4a21-b7fd-e2d9c4a7801c
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
/// 操作日志仓储实现
/// </summary>
public sealed class OperationLogRepository(
    ISqlSugarClientResolver clientResolver,
    ISplitTableLocator splitTableLocator)
    : SaasSplitRepository<SysOperationLog>(clientResolver, splitTableLocator), IOperationLogRepository;
