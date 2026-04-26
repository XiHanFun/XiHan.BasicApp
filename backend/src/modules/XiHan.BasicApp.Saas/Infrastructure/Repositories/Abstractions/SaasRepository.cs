#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasRepository
// Guid:7b0b84d5-bb91-4894-9dc1-7121a8bc0c53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
