// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 开通租户：<c>POST /api/tenants</c>。写 <see cref="SysTenant"/> 并在同一工作单元里登记框架租户目录（启用）。<c>platform</c> 是保留 ID。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsCreate)]
public sealed class CreateTenant(IRepository<SysTenant, Guid> tenants, TenantService service, IIdGenerator<Guid> ids) : IOperation<CreateTenantRequest, TenantResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.create";

    /// <inheritdoc />
    public async ValueTask<Result<TenantResponse>> HandleAsync(CreateTenantRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var tenantId = request.TenantId.Trim();
        if (string.Equals(tenantId, TenantIds.Platform, StringComparison.OrdinalIgnoreCase) || (await service.FindByTenantIdAsync(tenantId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(TenancyErrors.TenantIdTaken, TenancyMessages.TenantIdTaken, MessageArguments.Of(("tenantId", tenantId)));
        }

        var tenant = new SysTenant
        {
            Id = ids.NewId(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ExpiresAt = request.ExpiresAt,
            Order = request.Order,
        };
        var added = await tenants.AddAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var synced = await service.SyncDirectoryAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        return synced.IsFailure ? synced.Error! : TenantResponse.From(tenant);
    }
}
