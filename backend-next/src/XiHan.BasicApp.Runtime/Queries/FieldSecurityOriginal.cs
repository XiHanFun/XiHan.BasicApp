// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Queries;

/// <summary>
/// 字段安全修改判定要的原记录。
/// </summary>
/// <remarks>
/// 各包登记修改操作时用（<c>FieldSecurityEntityBuilder.Updates</c>）：按主键在调用的工作单元里取实体并投影成响应；
/// 找不到为空，交给操作自己报找不到。可见范围不在这里核对（只拿来比对，不对外返回）。
/// </remarks>
public static class FieldSecurityOriginal
{
    /// <summary>
    /// 按主键取实体并同步投影。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="context">调用上下文。</param>
    /// <param name="id">主键。</param>
    /// <param name="project">投影成响应。</param>
    /// <returns>响应对象；找不到为空。</returns>
    public static async ValueTask<object?> FindAsync<TEntity>(InvocationContext context, Guid id, Func<TEntity, object> project)
        where TEntity : class, IEntity<Guid>
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(project);
        var found = await context.Services.GetRequiredService<IRepository<TEntity, Guid>>().FindAsync(id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var entity) ? project(entity) : null;
    }

    /// <summary>
    /// 按主键取实体并异步投影。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="context">调用上下文。</param>
    /// <param name="id">主键。</param>
    /// <param name="project">投影成响应（可用作用域服务）。</param>
    /// <returns>响应对象；找不到为空。</returns>
    public static async ValueTask<object?> FindAsync<TEntity>(InvocationContext context, Guid id, Func<TEntity, InvocationContext, Task<object>> project)
        where TEntity : class, IEntity<Guid>
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(project);
        var found = await context.Services.GetRequiredService<IRepository<TEntity, Guid>>().FindAsync(id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var entity) ? await project(entity, context).ConfigureAwait(false) : null;
    }
}
