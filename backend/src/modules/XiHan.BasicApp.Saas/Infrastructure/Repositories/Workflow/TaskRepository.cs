#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskRepository
// Guid:c5c5b7d7-3622-4c0d-9253-5bd18e6274f7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 任务仓储实现
/// </summary>
public sealed class TaskRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysTask>(clientResolver, unitOfWorkManager), ITaskRepository;
