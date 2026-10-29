// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 一个租户的配额与用量。
/// </summary>
/// <param name="TenantId">租户 ID。</param>
/// <param name="Name">租户名称。</param>
/// <param name="EditionCode">套餐编码；没有为空。</param>
/// <param name="EditionName">套餐名称。</param>
/// <param name="UserLimit">有效的席位上限（租户自己的，没有取套餐的）；为空不限。</param>
/// <param name="UsedSeats">已用席位（注册地是该租户的用户与租户成员）。</param>
/// <param name="StorageLimitMb">有效的存储上限（MB）；为空不限。</param>
/// <param name="UsedStorageBytes">已用存储（字节）。</param>
/// <param name="IsOverSeats">席位是否超额。</param>
/// <param name="IsOverStorage">存储是否超额。</param>
public sealed record TenantQuotaResponse(
    string TenantId,
    string Name,
    string? EditionCode,
    string? EditionName,
    int? UserLimit,
    long UsedSeats,
    long? StorageLimitMb,
    long UsedStorageBytes,
    bool IsOverSeats,
    bool IsOverStorage)
{
    /// <summary>
    /// 由配额视图投影。
    /// </summary>
    /// <param name="view">配额与用量。</param>
    /// <returns>响应。</returns>
    public static TenantQuotaResponse From(TenantQuotaView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new(view.Tenant.TenantId, view.Tenant.Name, view.Edition?.Code, view.Edition?.Name, view.UserLimit, view.UsedSeats, view.StorageLimitMb, view.UsedStorageBytes, view.IsOverSeats, view.IsOverStorage);
    }
}

/// <summary>
/// <c>tenants.quotas</c> 的请求。
/// </summary>
/// <param name="OverOnly">只列超额的（配额审计）。</param>
public sealed record ListTenantQuotasRequest(bool OverOnly = false);

/// <summary>
/// <c>tenants.quotas</c> 的响应。
/// </summary>
/// <param name="Items">各租户的配额与用量（按排序号、再按租户 ID）。</param>
public sealed record TenantQuotasResponse(IReadOnlyList<TenantQuotaResponse> Items);

/// <summary>
/// 各租户的有效配额与用量。
/// </summary>
/// <remarks>
/// <c>GET /api/tenants:quotas</c>（4.x 的租户列表用量列与配额审计）。只在平台侧。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="quotas">租户配额。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = TenancyPermissions.TenantsRead)]
public sealed class ListTenantQuotas(IRepository<SysTenant, Guid> tenants, TenantQuotas quotas) : IOperation<ListTenantQuotasRequest, TenantQuotasResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.quotas";

    /// <inheritdoc />
    public async ValueTask<Result<TenantQuotasResponse>> HandleAsync(ListTenantQuotasRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var all = await tenants.ListAsync(EntityQuery.All<SysTenant>(), context.CancellationToken).ConfigureAwait(false);
        var views = await quotas.ViewsAsync([.. all.OrderBy(static t => t.Order).ThenBy(static t => t.TenantId, StringComparer.Ordinal)], context.CancellationToken).ConfigureAwait(false);
        return new TenantQuotasResponse([.. views.Where(v => !request.OverOnly || v.IsOverSeats || v.IsOverStorage).Select(TenantQuotaResponse.From)]);
    }
}
