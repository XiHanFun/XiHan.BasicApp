#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MigrationHistoryRepository
// Guid:f56dba1f-dc48-4673-9c9a-68a8f791cdcf
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
/// 迁移历史仓储实现
/// </summary>
public sealed class MigrationHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysMigrationHistory>(clientResolver), IMigrationHistoryRepository;
