// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 修改套餐：<c>PUT /api/editions/{id}</c>（平台侧）。只动名称、说明、缺省标记与排序，不影响门控（白名单走 <c>editions.set-permissions</c>）。
/// </summary>
/// <param name="editions">套餐仓储。</param>
/// <param name="service">套餐域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.EditionsUpdate)]
public sealed class UpdateEdition(IRepository<SysEdition, Guid> editions, EditionService service) : IOperation<UpdateEditionRequest, EditionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "editions.update";

    /// <inheritdoc />
    public async ValueTask<Result<EditionDetailResponse>> HandleAsync(UpdateEditionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var edition))
        {
            return Error.NotFound(TenancyErrors.EditionNotFound, TenancyMessages.EditionNotFound, MessageArguments.Of(("id", request.Id)));
        }

        edition.Name = request.Name.Trim();
        edition.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        edition.IsDefault = request.IsDefault;
        edition.Order = request.Order;
        var updated = await editions.UpdateAsync(edition, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        if (edition.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(edition.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        return await EditionDetailResponse.LoadAsync(service, edition, context.CancellationToken).ConfigureAwait(false);
    }
}
