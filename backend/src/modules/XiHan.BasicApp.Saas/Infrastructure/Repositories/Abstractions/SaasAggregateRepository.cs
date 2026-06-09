#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasAggregateRepository
// Guid:2e2a9b84-a052-4a10-b6d1-66adcf99b383
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Domain.Aggregates.Abstracts;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// SaaS SqlSugar 聚合根仓储基类
/// </summary>
/// <typeparam name="TAggregateRoot">聚合根类型</typeparam>
public abstract class SaasAggregateRepository<TAggregateRoot>(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SqlSugarAggregateRepository<TAggregateRoot, long>(clientResolver, unitOfWorkManager), ISaasAggregateRepository<TAggregateRoot>, IScopedDependency
    where TAggregateRoot : class, IAggregateRoot<long>, new();
