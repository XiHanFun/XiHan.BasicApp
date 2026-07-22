// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
