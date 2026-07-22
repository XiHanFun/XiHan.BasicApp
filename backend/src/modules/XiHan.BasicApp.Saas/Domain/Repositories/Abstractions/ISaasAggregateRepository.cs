// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Aggregates.Abstracts;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// SaaS 聚合根仓储接口
/// </summary>
/// <typeparam name="TAggregateRoot">聚合根类型</typeparam>
public interface ISaasAggregateRepository<TAggregateRoot> : IAggregateRootRepository<TAggregateRoot, long>
    where TAggregateRoot : class, IAggregateRoot<long>, new()
{
}
