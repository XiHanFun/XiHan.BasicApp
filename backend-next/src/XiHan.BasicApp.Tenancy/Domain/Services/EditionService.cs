// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Domain.Services;

/// <summary>
/// 套餐域服务：按编码 / 缺省查找、白名单的校验与整体替换、缺省标记互斥、哪些租户在用。
/// 白名单只能放租户能生效的权限（作用侧 Tenant / Both）：目录取各包登记的 <see cref="PermissionCatalogSection"/>（Tenancy 不引用 Access）。
/// </summary>
/// <param name="editions">套餐仓储。</param>
/// <param name="permissions">白名单仓储。</param>
/// <param name="tenants">租户仓储。</param>
/// <param name="sections">各包登记的权限目录片段。</param>
/// <param name="ids">标识生成器。</param>
public sealed class EditionService(
    IRepository<SysEdition, Guid> editions,
    IRepository<SysEditionGrant, Guid> permissions,
    IRepository<SysTenant, Guid> tenants,
    IEnumerable<PermissionCatalogSection> sections,
    IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 套餐白名单最多的权限数。
    /// </summary>
    public const int MaxPermissions = 500;

    /// <summary>
    /// 租户能生效的全部权限（作用侧 Tenant / Both），按权限码排序。
    /// </summary>
    public IReadOnlyList<PermissionDescriptor> TenantEffective { get; } = sections
        .SelectMany(static s => s.Permissions)
        .Where(static p => p.Scope is PermissionScope.Tenant or PermissionScope.Both)
        .OrderBy(static p => p.Code, StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// 按主键查找。
    /// </summary>
    public ValueTask<Maybe<SysEdition>> FindAsync(Guid id, CancellationToken cancellationToken) => editions.FindAsync(id, cancellationToken);

    /// <summary>
    /// 按编码查找（区分大小写，编码本身只有小写）。
    /// </summary>
    public async Task<Maybe<SysEdition>> FindByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var rows = await editions.ListAsync(EntityQuery.All<SysEdition>().Where(new Specification<SysEdition>(e => e.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysEdition>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 缺省套餐；没有即空。
    /// </summary>
    public async Task<Maybe<SysEdition>> FindDefaultAsync(CancellationToken cancellationToken)
    {
        var rows = await editions.ListAsync(EntityQuery.All<SysEdition>().Where(new Specification<SysEdition>(e => e.IsDefault)).OrderBy(static e => e.Order).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysEdition>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 套餐的白名单（权限码，排序）。
    /// </summary>
    public async Task<IReadOnlyList<string>> PermissionsOfAsync(Guid editionId, CancellationToken cancellationToken)
    {
        var rows = await permissions.ListAsync(EntityQuery.All<SysEditionGrant>().Where(new Specification<SysEditionGrant>(p => p.EditionId == editionId)), cancellationToken).ConfigureAwait(false);
        return rows.Select(static p => p.PermissionCode).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 校验白名单：去重后每个码都须是租户能生效的权限；第一个不合格的码返回 <c>TENANCY_VALIDATION_002</c>。
    /// </summary>
    public Result<IReadOnlyList<string>> Normalize(IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var allowed = TenantEffective.Select(static p => p.Code).ToHashSet(StringComparer.Ordinal);
        var distinct = codes.Select(static c => c.Trim()).Distinct(StringComparer.Ordinal).ToList();
        foreach (var code in distinct)
        {
            if (!allowed.Contains(code))
            {
                return Result.Failure<IReadOnlyList<string>>(Error.Validation(TenancyErrors.EditionPermissionInvalid, TenancyMessages.EditionPermissionInvalid, MessageArguments.Of(("code", code))));
            }
        }

        return Result.Success<IReadOnlyList<string>>(distinct.Order(StringComparer.Ordinal).ToList());
    }

    /// <summary>
    /// 把白名单整体替换为 <paramref name="codes"/>（已校验）：多的删、少的补，没变的行不动。
    /// </summary>
    public async Task<Result> ReplacePermissionsAsync(Guid editionId, IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var current = await permissions.ListAsync(EntityQuery.All<SysEditionGrant>().Where(new Specification<SysEditionGrant>(p => p.EditionId == editionId)), cancellationToken).ConfigureAwait(false);
        var wanted = codes.ToHashSet(StringComparer.Ordinal);
        var removing = current.Where(p => !wanted.Contains(p.PermissionCode)).ToList();
        if (removing.Count > 0)
        {
            foreach (var removed in await permissions.RemoveRangeAsync(removing, cancellationToken).ConfigureAwait(false))
            {
                if (removed.IsFailure)
                {
                    return removed;
                }
            }
        }

        var present = current.Select(static p => p.PermissionCode).ToHashSet(StringComparer.Ordinal);
        var adding = codes.Where(c => !present.Contains(c)).Select(c => new SysEditionGrant { Id = ids.NewId(), EditionId = editionId, PermissionCode = c }).ToList();
        if (adding.Count > 0)
        {
            foreach (var added in await permissions.AddRangeAsync(adding, cancellationToken).ConfigureAwait(false))
            {
                if (added.IsFailure)
                {
                    return added;
                }
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 清掉 <paramref name="keepId"/> 之外的缺省标记（缺省套餐至多一个）。
    /// </summary>
    public async Task<Result> ClearOtherDefaultsAsync(Guid keepId, CancellationToken cancellationToken)
    {
        var others = await editions.ListAsync(EntityQuery.All<SysEdition>().Where(new Specification<SysEdition>(e => e.IsDefault && e.Id != keepId)), cancellationToken).ConfigureAwait(false);
        if (others.Count == 0)
        {
            return Result.Success();
        }

        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        foreach (var updated in await editions.UpdateRangeAsync(others, cancellationToken).ConfigureAwait(false))
        {
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 绑定该套餐的租户 ID（目录键）。
    /// </summary>
    public async Task<IReadOnlyList<string>> TenantsUsingAsync(Guid editionId, CancellationToken cancellationToken)
    {
        var rows = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.EditionId == editionId)), cancellationToken).ConfigureAwait(false);
        return rows.Select(static t => t.TenantId).ToList();
    }

    /// <summary>
    /// 各套餐的白名单条数与绑定租户数（列表用）。
    /// </summary>
    public async Task<(IReadOnlyDictionary<Guid, int> Permissions, IReadOnlyDictionary<Guid, int> Tenants)> CountsAsync(CancellationToken cancellationToken)
    {
        var allPermissions = await permissions.ListAsync(EntityQuery.All<SysEditionGrant>(), cancellationToken).ConfigureAwait(false);
        var bound = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.EditionId.HasValue)), cancellationToken).ConfigureAwait(false);
        return (
            allPermissions.GroupBy(static p => p.EditionId).ToDictionary(static g => g.Key, static g => g.Count()),
            bound.GroupBy(static t => t.EditionId!.Value).ToDictionary(static g => g.Key, static g => g.Count()));
    }
}
