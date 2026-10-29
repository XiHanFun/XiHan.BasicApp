// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 批量删除套餐。
/// </summary>
/// <remarks>
/// <c>DELETE /api/editions?ids=...</c>（平台侧）。还有租户绑定的套餐不能删（逐项 <c>TENANCY_CONFLICT_007</c>）；删除连同白名单。删掉缺省套餐后开通租户就必须指定套餐。
/// </remarks>
/// <param name="editions">套餐仓储。</param>
/// <param name="service">套餐域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.EditionsDelete)]
public sealed class DeleteEditions(IRepository<SysEdition, Guid> editions, EditionService service) : IOperation<EditionIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "editions.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(EditionIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var found = (await editions.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static e => e.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var edition))
            {
                failed.Add(new BatchFailure(id, TenancyErrors.EditionNotFound));
                continue;
            }

            if ((await service.TenantsUsingAsync(id, context.CancellationToken).ConfigureAwait(false)).Count > 0)
            {
                failed.Add(new BatchFailure(id, TenancyErrors.EditionInUse));
                continue;
            }

            var cleared = await service.ReplacePermissionsAsync(id, [], context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }

            var removed = await editions.RemoveAsync(edition, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
