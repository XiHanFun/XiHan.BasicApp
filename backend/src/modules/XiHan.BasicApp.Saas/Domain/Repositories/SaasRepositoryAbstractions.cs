#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasRepositoryAbstractions
// Guid:ba189fc8-009b-4d5f-98e6-d7d19877dcfd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Aggregates.Abstracts;
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

/// <summary>
/// SaaS 聚合根仓储接口
/// </summary>
/// <typeparam name="TAggregateRoot">聚合根类型</typeparam>
public interface ISaasAggregateRepository<TAggregateRoot> : IAggregateRootRepository<TAggregateRoot, long>
    where TAggregateRoot : class, IAggregateRoot<long>, new()
{
}
