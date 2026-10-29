// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Queries;

/// <summary>
/// 数据范围：取当前主体的判定并套到 SqlSugar 查询上。
/// </summary>
/// <remarks>
/// 方案 5.6：判定按「归属人列」或「部门列」套到查询上。
/// 平台侧与没有装 Access 的宿主一律不受限；受限时生成 <c>owner IN (...) [OR department IN (...)]</c>。
/// </remarks>
public static class DataScopeQuery
{
    /// <summary>
    /// 当前主体的判定：有 <see cref="IDataScopeResolver"/> 就问它，没有即不受限。
    /// </summary>
    public static ValueTask<DataScopeDecision> ResolveAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var resolver = context.Services.GetService<IDataScopeResolver>();
        return resolver is null ? ValueTask.FromResult(DataScopeDecision.Everything) : resolver.ResolveAsync(context);
    }

    /// <summary>
    /// 把数据范围判定套到查询上。
    /// </summary>
    /// <param name="query">查询。</param>
    /// <param name="decision">数据范围判定。</param>
    /// <param name="owner">行的归属人列。</param>
    /// <param name="department">行的部门列（可选），命中可见部门的行也放行。</param>
    public static ISugarQueryable<TEntity> ApplyDataScope<TEntity>(this ISugarQueryable<TEntity> query, DataScopeDecision decision, Expression<Func<TEntity, Guid>> owner, Expression<Func<TEntity, Guid>>? department = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(owner);
        if (decision.Unrestricted)
        {
            return query;
        }

        var entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression body = In(decision.UserIds.ToList(), new ParameterReplacer(owner.Parameters[0], entity).Visit(owner.Body));
        if (department is not null && decision.DepartmentIds.Count > 0)
        {
            body = Expression.OrElse(body, In(decision.DepartmentIds.ToList(), new ParameterReplacer(department.Parameters[0], entity).Visit(department.Body)));
        }

        return query.Where(Expression.Lambda<Func<TEntity, bool>>(body, entity));
    }

    private static MethodCallExpression In(List<Guid> values, Expression member)
    {
        var contains = typeof(Enumerable).GetMethods().First(static m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2).MakeGenericMethod(typeof(Guid));
        return Expression.Call(contains, Expression.Constant(values), member);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
