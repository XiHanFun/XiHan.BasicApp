// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 已登记仓储的实体类型。
/// </summary>
/// <remarks>
/// 框架的建表清单不公开，这里从仓储登记表重放出来：直接遍历登记项喂给只记类型的登记器，不改登记表的状态。
/// 容器建好之后第一次取用时才收集，那时各包都已登记完。租户库只建实现 <see cref="IMultiTenant"/> 的实体（方案 D3、D6）。
/// </remarks>
public sealed class EntityTypeCatalog
{
    private EntityTypeCatalog(IReadOnlyList<Type> all)
    {
        All = all;
        TenantScoped = all.Where(static t => typeof(IMultiTenant).IsAssignableFrom(t)).ToList();
    }

    /// <summary>
    /// 全部实体类型。
    /// </summary>
    public IReadOnlyList<Type> All { get; }

    /// <summary>
    /// 落租户库的实体类型。
    /// </summary>
    public IReadOnlyList<Type> TenantScoped { get; }

    /// <summary>
    /// 从仓储登记表收集。
    /// </summary>
    public static EntityTypeCatalog From(RepositoryRegistrations registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        var recorder = new Recorder();
        var scratch = new ServiceCollection();
        foreach (var registration in registrations.Items)
        {
            registration(scratch, recorder);
        }

        return new EntityTypeCatalog(recorder.Types.Distinct().ToList());
    }

    private sealed class Recorder : IRepositoryRegistrar
    {
        public List<Type> Types { get; } = [];

        public void Register<TEntity, TKey>(IServiceCollection services)
            where TEntity : class, IEntity<TKey>
            where TKey : notnull, IEquatable<TKey> => Types.Add(typeof(TEntity));
    }
}
