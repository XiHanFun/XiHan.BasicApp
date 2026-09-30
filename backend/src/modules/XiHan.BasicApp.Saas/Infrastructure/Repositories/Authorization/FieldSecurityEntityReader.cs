// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 按实体类型读取当前行：借框架只读仓储走同一套租户与软删过滤
/// </summary>
public sealed class FieldSecurityEntityReader(ISqlSugarClientResolver clientResolver) : IFieldSecurityEntityReader
{
    private static readonly MethodInfo FindCoreMethod = typeof(FieldSecurityEntityReader)
        .GetMethod(nameof(FindCoreAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> FindMethods = new();

    /// <inheritdoc />
    public Task<object?> FindAsync(Type entityType, long id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        var method = FindMethods.GetOrAdd(entityType, static type => FindCoreMethod.MakeGenericMethod(type));
        return (Task<object?>)method.Invoke(this, [id, cancellationToken])!;
    }

    private async Task<object?> FindCoreAsync<TEntity>(long id, CancellationToken cancellationToken)
        where TEntity : class, IEntityBase<long>, new()
    {
        return await new SqlSugarReadOnlyRepository<TEntity, long>(clientResolver).GetByIdAsync(id, cancellationToken);
    }
}
