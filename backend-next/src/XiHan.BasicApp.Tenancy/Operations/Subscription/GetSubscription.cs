// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Subscription;

/// <summary>
/// <c>subscription.get</c> 的请求（取调用方所在的租户，没有参数）。
/// </summary>
public sealed record GetSubscriptionRequest;

/// <summary>
/// 本租户的订阅。
/// </summary>
/// <param name="TenantId">租户 ID。</param>
/// <param name="TenantName">租户名称。</param>
/// <param name="Status">租户状态。</param>
/// <param name="ExpiresAt">到期时间；为空即长期有效。</param>
/// <param name="IsExpired">是否已到期。</param>
/// <param name="EditionCode">套餐编码；没有套餐为空。</param>
/// <param name="EditionName">套餐名称。</param>
/// <param name="EditionDescription">套餐说明。</param>
/// <param name="IsFree">是否免费套餐。</param>
/// <param name="Price">套餐价格；为空不标价。</param>
/// <param name="BillingPeriodMonths">计费周期（月）。</param>
/// <param name="UserLimit">有效的席位上限；为空不限。</param>
/// <param name="UsedSeats">已用席位。</param>
/// <param name="StorageLimitMb">有效的存储上限（MB）；为空不限。</param>
/// <param name="UsedStorageBytes">已用存储（字节）。</param>
public sealed record SubscriptionResponse(
    string TenantId,
    string TenantName,
    TenantStatus Status,
    DateTimeOffset? ExpiresAt,
    bool IsExpired,
    string? EditionCode,
    string? EditionName,
    string? EditionDescription,
    bool IsFree,
    decimal? Price,
    int? BillingPeriodMonths,
    int? UserLimit,
    long UsedSeats,
    long? StorageLimitMb,
    long UsedStorageBytes);

/// <summary>
/// 本租户的订阅。
/// </summary>
/// <remarks>
/// <c>GET /api/subscription</c>（4.x 的「我的订阅」）。只在租户侧；价格与周期只作展示，没有续费与付款流程。
/// </remarks>
/// <param name="service">租户域服务。</param>
/// <param name="quotas">租户配额。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.SubscriptionRead)]
public sealed class GetSubscription(TenantService service, TenantQuotas quotas, TimeProvider clock) : IOperation<GetSubscriptionRequest, SubscriptionResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "subscription.get";

    /// <inheritdoc />
    public async ValueTask<Result<SubscriptionResponse>> HandleAsync(GetSubscriptionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        if (tenantId is null)
        {
            return Error.Denied(TenancyErrors.SubscriptionTenantOnly, TenancyMessages.SubscriptionTenantOnly);
        }

        if (!(await service.FindByTenantIdAsync(tenantId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var tenant))
        {
            return Error.NotFound(TenancyErrors.TenantNotFound, TenancyMessages.TenantNotFound, MessageArguments.Of(("id", tenantId)));
        }

        var view = (await quotas.ViewsAsync([tenant], context.CancellationToken).ConfigureAwait(false))[0];
        var edition = view.Edition;
        return new SubscriptionResponse(
            tenant.TenantId,
            tenant.Name,
            tenant.Status,
            tenant.ExpiresAt,
            tenant.ExpiresAt is { } expiresAt && expiresAt <= clock.GetUtcNow(),
            edition?.Code,
            edition?.Name,
            edition?.Description,
            edition?.IsFree ?? false,
            edition?.Price,
            edition?.BillingPeriodMonths,
            view.UserLimit,
            view.UsedSeats,
            view.StorageLimitMb,
            view.UsedStorageBytes);
    }
}
