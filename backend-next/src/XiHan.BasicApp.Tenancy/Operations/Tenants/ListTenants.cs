// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.BasicApp.Tenancy.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 租户列表：<c>POST /api/tenants:list</c>（分页、关键字、筛选、排序）。
/// </summary>
/// <param name="queries">租户查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsRead)]
public sealed class ListTenants(TenantQueries queries) : IOperation<PageRequest, PageResult<TenantResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<TenantResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return TenantService.RequirePlatform(context) is { } denied ? ValueTask.FromResult(Result.Failure<PageResult<TenantResponse>>(denied)) : queries.ListAsync(request, context);
    }
}
