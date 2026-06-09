#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISaasAggregateRepository
// Guid:67299d8a-42ea-42f0-8819-1cf10645bb01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
