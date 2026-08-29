// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Sample.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Sample.Infrastructure.Repositories;

/// <summary>
/// 示例便签仓储，落主库。
/// </summary>
/// <remarks>
/// 继承 <see cref="SaasRepository{TEntity}"/> 即自动注册进容器（<c>IScopedDependency</c>），
/// 不需要在模块里手写 DI 登记。
/// </remarks>
public class SampleNoteRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SampleNote>(clientResolver);
