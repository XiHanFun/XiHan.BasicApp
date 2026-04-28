#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasSplitRepository
// Guid:21b1f734-6e91-4f4f-a76d-05cd5b6ad86b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// SaaS SqlSugar 分表实体仓储基类
/// </summary>
/// <typeparam name="TEntity">分表实体类型</typeparam>
public abstract class SaasSplitRepository<TEntity>(
    ISqlSugarClientResolver clientResolver,
    ISplitTableLocator splitTableLocator)
    : SqlSugarSplitRepository<TEntity>(clientResolver, splitTableLocator), ISaasSplitRepository<TEntity>, IScopedDependency
    where TEntity : class, IEntityBase<long>, ISplitTableEntity, new();
