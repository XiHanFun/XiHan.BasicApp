// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Sample.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Sample.Infrastructure.Repositories;

/// <summary>
/// 示例 Erp 订单仓储，落 Erp 库。
/// </summary>
/// <remarks>
/// 仓储写法与落主库的仓储**完全一样**——落哪个库由实体上的 <c>[DataSource]</c> 决定，
/// 这一层不需要知道。
/// </remarks>
public class SampleErpOrderRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SampleErpOrder>(clientResolver);
