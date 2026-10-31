// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 停用 / 恢复租户的共同骨架。
/// </summary>
/// <remarks>
/// 逐个找租户、置状态、写回并同步租户目录；已经是目标状态的算成功，<c>refuse</c> 给出错误码的记为逐项失败。
/// </remarks>
internal static class TenantStatusBatch
{
    /// <summary>
    /// 执行一批。
    /// </summary>
    public static async ValueTask<Result<BatchResult>> RunAsync(TenantIdsRequest request, InvocationContext context, IRepository<SysTenant, Guid> tenants, TenantService service, TenantStatus target, Func<SysTenant, string?>? refuse = null)
    {
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var found = (await tenants.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static t => t.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var tenant))
            {
                failed.Add(new BatchFailure(id, TenancyErrors.TenantNotFound));
                continue;
            }

            if (tenant.Status != target)
            {
                if (refuse?.Invoke(tenant) is { } code)
                {
                    failed.Add(new BatchFailure(id, code));
                    continue;
                }

                tenant.Status = target;
                var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    return updated.Error!;
                }

                var synced = await service.SyncDirectoryAsync(tenant, context.CancellationToken).ConfigureAwait(false);
                if (synced.IsFailure)
                {
                    return synced.Error!;
                }
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
