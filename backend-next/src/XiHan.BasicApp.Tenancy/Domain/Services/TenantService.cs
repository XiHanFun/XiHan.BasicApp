// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.MultiTenancy.Database.Tenants;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Domain.Services;

/// <summary>
/// 租户域服务（作用域）。
/// </summary>
/// <remarks>
/// 平台侧校验、按 ID / 租户 ID 找租户、与框架租户目录（<c>TenantRecord</c>）同步。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="directory">框架租户目录存储。</param>
public sealed class TenantService(IRepository<SysTenant, Guid> tenants, TenantRecordStore directory)
{
    /// <summary>
    /// 租户管理只对平台主体开放：带租户声明的调用一律拒绝（权限码本身也只在平台侧）。
    /// </summary>
    public static Error? RequirePlatform(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant is null ? null : Error.Denied(TenancyErrors.PlatformOnly, TenancyMessages.PlatformOnly);
    }

    /// <summary>
    /// 按主键找租户。
    /// </summary>
    public ValueTask<Maybe<SysTenant>> FindAsync(Guid id, CancellationToken cancellationToken) => tenants.FindAsync(id, cancellationToken);

    /// <summary>
    /// 按租户 ID（目录键）找租户。
    /// </summary>
    public async Task<Maybe<SysTenant>> FindByTenantIdAsync(string tenantId, CancellationToken cancellationToken)
    {
        var rows = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.TenantId == tenantId)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysTenant>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 把租户的目录字段（ID、显示名、是否启用）写进框架租户目录。
    /// </summary>
    /// <remarks>
    /// 目录缓存随之失效。
    /// </remarks>
    public async Task<Result> SyncDirectoryAsync(SysTenant tenant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var info = new TenantInfo { Id = tenant.TenantId, DisplayName = tenant.Name, IsEnabled = tenant.Status == TenantStatus.Active };
        foreach (var outcome in await directory.UpsertAsync([info], cancellationToken).ConfigureAwait(false))
        {
            if (outcome.Outcome.IsFailure)
            {
                return outcome.Outcome;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 从框架租户目录移除（删除租户时）。
    /// </summary>
    /// <remarks>
    /// 目录里本来没有也算成功。
    /// </remarks>
    public async Task<Result> RemoveFromDirectoryAsync(string tenantId, CancellationToken cancellationToken)
    {
        foreach (var outcome in await directory.RemoveAsync([tenantId], cancellationToken).ConfigureAwait(false))
        {
            if (outcome.Outcome.IsFailure && !string.Equals(outcome.Outcome.Error!.Kind, "NOTFOUND", StringComparison.Ordinal))
            {
                return outcome.Outcome;
            }
        }

        return Result.Success();
    }
}
