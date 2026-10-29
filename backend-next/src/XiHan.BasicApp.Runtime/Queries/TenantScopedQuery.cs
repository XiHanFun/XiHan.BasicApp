// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Queries;

/// <summary>
/// 包内直接查库的起手：按实体落点取连接并补条件。
/// </summary>
/// <remarks>
/// 包内 <c>Queries/</c> 直接用 <c>ISqlSugarClient</c> 的分页、投影、联表必须以此起手：按实体落点取连接，补软删除与租户条件（框架没有全局过滤器）。
/// </remarks>
public static class TenantScopedQuery
{
    /// <summary>
    /// 按实体落点（默认连接或 <c>shard</c>）取连接并补条件。
    /// </summary>
    /// <remarks>
    /// 实现 <c>ISoftDeletable</c> 的只看未删除行；实现 <c>IMultiTenant</c> 的只看当前租户的行，无租户的调用看不到任何行。
    /// </remarks>
    public static ISugarQueryable<TEntity> For<TEntity>(InvocationContext context)
        where TEntity : class, new()
    {
        ArgumentNullException.ThrowIfNull(context);
        var unitOfWork = context.Services.GetRequiredService<SqlSugarUnitOfWork>();
        return For<TEntity>(unitOfWork.ConnectionFor(typeof(TEntity)), context);
    }

    /// <summary>
    /// 在指定连接上起手并补软删除与租户条件。
    /// </summary>
    public static ISugarQueryable<TEntity> For<TEntity>(ISqlSugarClient client, InvocationContext context)
        where TEntity : class, new()
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(context);
        var query = client.Queryable<TEntity>();
        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(Equals<TEntity, bool>(nameof(ISoftDeletable.IsDeleted), false));
        }

        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
        {
            var tenantId = context.GetSecurity().Tenant?.Id;
            query = tenantId is null
                ? query.Where("1 = 0", (object?)null)
                : query.Where(Equals<TEntity, string>(nameof(IMultiTenant.TenantId), tenantId));
        }

        return query;
    }

    /// <summary>
    /// 读共享：平台的记录加当前租户的记录。
    /// </summary>
    /// <remarks>
    /// 没有当前租户时只有平台的。
    /// </remarks>
    public static ISugarQueryable<TEntity> OwnedByCurrentTenantOrPlatform<TEntity>(this ISugarQueryable<TEntity> query, InvocationContext context)
        where TEntity : class, IOwnedByTenant, new()
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        return tenantId is null
            ? query.Where(Equals<TEntity, string>(nameof(IOwnedByTenant.OwnerTenantId), TenantIds.Platform))
            : query.Where(In<TEntity>(nameof(IOwnedByTenant.OwnerTenantId), [TenantIds.Platform, tenantId]));
    }

    /// <summary>
    /// 写不共享：只有当前租户自己的记录。
    /// </summary>
    /// <remarks>
    /// 没有当前租户时只有平台的。
    /// </remarks>
    public static ISugarQueryable<TEntity> OwnedByCurrentTenant<TEntity>(this ISugarQueryable<TEntity> query, InvocationContext context)
        where TEntity : class, IOwnedByTenant, new()
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        var owner = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        return query.Where(Equals<TEntity, string>(nameof(IOwnedByTenant.OwnerTenantId), owner));
    }

    private static Expression<Func<TEntity, bool>> Equals<TEntity, TValue>(string property, TValue value)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Expression.Property(entity, property), Expression.Constant(value, typeof(TValue))), entity);
    }

    private static Expression<Func<TEntity, bool>> In<TEntity>(string property, string[] values)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var contains = typeof(Enumerable).GetMethods().First(static m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2).MakeGenericMethod(typeof(string));
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Call(contains, Expression.Constant(values), Expression.Property(entity, property)), entity);
    }
}
