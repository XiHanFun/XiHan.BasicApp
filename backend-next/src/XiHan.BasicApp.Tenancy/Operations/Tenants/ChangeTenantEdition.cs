// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.BasicApp.Tenancy.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 给租户换套餐：<c>POST /api/tenants/{id}:change-edition</c>（平台侧，<c>tenancy.tenants.update</c>）。
/// 套餐须存在（<c>TENANCY_NOTFOUND_004</c>）；写入后失效该租户的门控缓存，其用户的有效权限随即按新白名单计算。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
/// <param name="editions">套餐域服务。</param>
/// <param name="gate">套餐门控。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsUpdate)]
public sealed class ChangeTenantEdition(IRepository<SysTenant, Guid> tenants, TenantService service, EditionService editions, EditionGate gate) : IOperation<ChangeTenantEditionRequest, TenantResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.change-edition";

    /// <inheritdoc />
    public async ValueTask<Result<TenantResponse>> HandleAsync(ChangeTenantEditionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var tenant))
        {
            return Error.NotFound(TenancyErrors.TenantNotFound, TenancyMessages.TenantNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (request.EditionId is { } editionId && !(await editions.FindAsync(editionId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(TenancyErrors.EditionNotFound, TenancyMessages.EditionNotFound, MessageArguments.Of(("id", editionId)));
        }

        tenant.EditionId = request.EditionId;
        var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await gate.InvalidateAsync([tenant.TenantId], context.CancellationToken).ConfigureAwait(false);
        return TenantResponse.From(tenant);
    }
}
