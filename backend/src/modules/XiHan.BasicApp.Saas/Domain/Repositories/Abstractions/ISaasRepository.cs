// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// SaaS 实体仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface ISaasRepository<TEntity> : IRepositoryBase<TEntity, long>
    where TEntity : class, IEntityBase<long>
{
}
