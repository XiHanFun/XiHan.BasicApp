// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Infrastructure;

/// <summary>
/// 套餐白名单里对一批权限码的引用
/// </summary>
/// <remarks>
/// 白名单在平台库；清掉之后用到这些套餐的租户的门控缓存随之失效
/// </remarks>
/// <param name="grants">白名单仓储</param>
/// <param name="editions">套餐仓储</param>
/// <param name="service">套餐域服务</param>
/// <param name="gate">套餐门控</param>
public sealed class EditionPermissionReferences(IRepository<SysEditionGrant, Guid> grants, IRepository<SysEdition, Guid> editions, EditionService service, EditionGate gate) : IPermissionReferences
{
    private const string PlatformSide = "platform";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionReference>>> FindAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var listed = await ListAsync(codes, cancellationToken).ConfigureAwait(false);
        if (listed.Count == 0)
        {
            return Result.Success<IReadOnlyList<PermissionReference>>([]);
        }

        var names = (await editions.FindManyAsync([.. listed.Select(static g => g.EditionId).Distinct()], cancellationToken).ConfigureAwait(false)).ToDictionary(static e => e.Id, static e => e.Code);
        return Result.Success<IReadOnlyList<PermissionReference>>([.. listed.Select(g => new PermissionReference(PermissionReferenceKind.Edition, PlatformSide, names.GetValueOrDefault(g.EditionId, g.EditionId.ToString("D")), g.PermissionCode))]);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var listed = await ListAsync(codes, cancellationToken).ConfigureAwait(false);
        if (listed.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await grants.RemoveRangeAsync(listed, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        foreach (var editionId in listed.Select(static g => g.EditionId).Distinct())
        {
            await gate.InvalidateAsync(await service.TenantsUsingAsync(editionId, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private Task<IReadOnlyList<SysEditionGrant>> ListAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
        => codes.Count == 0
            ? Task.FromResult<IReadOnlyList<SysEditionGrant>>([])
            : grants.ListAsync(EntityQuery.All<SysEditionGrant>().Where(new Specification<SysEditionGrant>(g => codes.Contains(g.PermissionCode))), cancellationToken);
}
