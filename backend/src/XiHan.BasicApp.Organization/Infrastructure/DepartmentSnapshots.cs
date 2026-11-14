// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Operations.Departments;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Organization.Infrastructure;

/// <summary>
/// 一个部门与它的直接成员数
/// </summary>
/// <param name="Department">部门</param>
/// <param name="Members">直接成员数</param>
public sealed record DepartmentEntry(DepartmentResponse Department, long Members);

/// <summary>
/// 一个租户全部部门的读穿缓存（作用域服务）
/// </summary>
/// <remarks>
/// 部门树与选择器都要整租户的部门和各部门的成员数，成员数要读全部归属行，按租户整份缓存
/// 部门是租户库实体，要在调用自己的租户上下文里读，所以不走 <see cref="StoreScope"/>：只在只读操作里读，写操作的事务里读会把未提交的行写进缓存
/// 放在共享缓存里 60 秒（只读工作单元可能读从库，回填了落后的数据也只停留这么久），部门与归属写入即失效
/// </remarks>
/// <param name="cache">共享缓存</param>
/// <param name="departments">部门仓储</param>
/// <param name="memberships">归属仓储</param>
public sealed class DepartmentSnapshots(ISharedCache cache, IRepository<SysDepartment, Guid> departments, IRepository<SysUserDepartment, Guid> memberships)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 当前租户的全部部门，按排序与名称排列
    /// </summary>
    /// <param name="tenantId">当前租户</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<IReadOnlyList<DepartmentEntry>> OfAsync(string tenantId, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(tenantId), LoadAsync, CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(raw, OrganizationJsonContext.Default.ListDepartmentEntry) ?? [];
    }

    /// <summary>
    /// 一个租户的部门失效
    /// </summary>
    /// <remarks>
    /// 调用里推到工作单元结束之后再删除（<see cref="CacheInvalidation"/>），提交前的旧数据不会被回填进缓存
    /// </remarks>
    public async Task InvalidateAsync(string tenantId, CancellationToken cancellationToken)
        => await CacheInvalidation.RemoveAsync(cache, KeyOf(tenantId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string tenantId) => new($"organization:departments:{tenantId}");

    private async ValueTask<string> LoadAsync(CancellationToken cancellationToken)
    {
        var rows = await departments.ListAsync(EntityQuery.All<SysDepartment>().OrderBy(static d => d.Order).OrderBy(static d => d.Name), cancellationToken).ConfigureAwait(false);
        var counts = (await memberships.ListAsync(EntityQuery.All<SysUserDepartment>(), cancellationToken).ConfigureAwait(false))
            .GroupBy(static m => m.DepartmentId)
            .ToDictionary(static g => g.Key, static g => (long)g.Count());
        var entries = rows.Select(d => new DepartmentEntry(DepartmentResponse.From(d), counts.GetValueOrDefault(d.Id))).ToList();
        return JsonSerializer.Serialize(entries, OrganizationJsonContext.Default.ListDepartmentEntry);
    }
}
