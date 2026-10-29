// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 修改租户资料：<c>PUT /api/tenants/{id}</c>；名称同步到租户目录的显示名。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsUpdate)]
public sealed class UpdateTenant(IRepository<SysTenant, Guid> tenants, TenantService service) : IOperation<UpdateTenantRequest, TenantResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.update";

    /// <inheritdoc />
    public async ValueTask<Result<TenantResponse>> HandleAsync(UpdateTenantRequest request, InvocationContext context)
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

        tenant.Name = request.Name.Trim();
        tenant.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        tenant.ExpiresAt = request.ExpiresAt;
        tenant.Order = request.Order;
        tenant.UserLimit = request.UserLimit;
        tenant.StorageLimitMb = request.StorageLimitMb;
        var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var synced = await service.SyncDirectoryAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        return synced.IsFailure ? synced.Error! : TenantResponse.From(tenant);
    }
}
