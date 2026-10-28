// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 套餐列表：<c>GET /api/editions</c>（平台侧），带白名单条数与绑定租户数。
/// </summary>
/// <param name="editions">套餐仓储。</param>
/// <param name="service">套餐域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.EditionsRead)]
public sealed class ListEditions(IRepository<SysEdition, Guid> editions, EditionService service) : IOperation<ListEditionsRequest, EditionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "editions.list";

    /// <inheritdoc />
    public async ValueTask<Result<EditionsResponse>> HandleAsync(ListEditionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var rows = await editions.ListAsync(EntityQuery.All<SysEdition>().OrderBy(static e => e.Order).OrderBy(static e => e.Code), context.CancellationToken).ConfigureAwait(false);
        var (permissions, tenants) = await service.CountsAsync(context.CancellationToken).ConfigureAwait(false);
        return new EditionsResponse(rows.Select(e => EditionResponse.From(e, permissions.GetValueOrDefault(e.Id), tenants.GetValueOrDefault(e.Id))).ToList());
    }
}
