// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// SaaS SqlSugar 普通实体仓储基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public abstract class SaasRepository<TEntity>(ISqlSugarClientResolver clientResolver)
    : SqlSugarRepositoryBase<TEntity, long>(clientResolver), ISaasRepository<TEntity>, IScopedDependency
    where TEntity : class, IEntityBase<long>, new();
