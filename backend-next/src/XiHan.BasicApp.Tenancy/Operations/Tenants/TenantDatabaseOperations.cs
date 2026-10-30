// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.BasicApp.Runtime.Security;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 设租户的隔离方式与独立库连接。
/// </summary>
/// <remarks>
/// <c>POST /api/tenants/{id}:configure-database</c>（平台侧），在初始化独立库之前。独立库就绪之后不能再改（数据已在那个库里，<c>TENANCY_CONFLICT_013</c>）。
/// 改成独立库即待初始化，连接串经 <see cref="ISecretProtector"/> 加密后存放并同步进租户目录；改回共库即清掉连接。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
/// <param name="protector">机密保护器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsConfigureDatabase)]
public sealed class ConfigureTenantDatabase(IRepository<SysTenant, Guid> tenants, TenantService service, ISecretProtector protector) : IOperation<ConfigureTenantDatabaseRequest, TenantResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.configure-database";

    /// <inheritdoc />
    public async ValueTask<Result<TenantResponse>> HandleAsync(ConfigureTenantDatabaseRequest request, InvocationContext context)
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

        if (tenant.DatabaseStatus == TenantDatabaseStatus.Ready)
        {
            return Error.Conflict(TenancyErrors.DatabaseInitialized, TenancyMessages.DatabaseInitialized, MessageArguments.Of(("tenantId", tenant.TenantId)));
        }

        if (request.IsolationMode == TenantIsolationMode.Field)
        {
            tenant.IsolationMode = TenantIsolationMode.Field;
            tenant.DatabaseProvider = null;
            tenant.ConnectionString = null;
            tenant.DatabaseStatus = TenantDatabaseStatus.NotRequired;
            tenant.DatabaseError = null;
        }
        else
        {
            var connection = request.ConnectionString?.Trim();
            if (string.IsNullOrEmpty(connection) && string.IsNullOrEmpty(tenant.ConnectionString))
            {
                return Error.Validation(TenancyErrors.ConnectionRequired, TenancyMessages.ConnectionRequired);
            }

            tenant.IsolationMode = TenantIsolationMode.Database;
            tenant.DatabaseProvider = string.IsNullOrWhiteSpace(request.DatabaseProvider) ? null : request.DatabaseProvider;
            if (!string.IsNullOrEmpty(connection))
            {
                tenant.ConnectionString = protector.Protect(ProtectedTenantShardResolver.Purpose, connection);
            }

            tenant.DatabaseStatus = TenantDatabaseStatus.Pending;
            tenant.DatabaseError = null;
        }

        var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var synced = await service.SyncDirectoryAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        return synced.IsFailure ? synced.Error! : TenantResponse.From(tenant);
    }
}

/// <summary>
/// 初始化租户的独立库。
/// </summary>
/// <remarks>
/// <c>POST /api/tenants/{id}:initialize-database</c>（平台侧）：按租户目录绑定该租户的分片（框架 <c>TenantShardScope</c>，停用的租户也行），
/// 建库、只建落租户库的实体的表、在该库登记当前结构版本（<see cref="TenantSchemas"/>，幂等）。
/// 结果写回租户：成功即就绪，失败记原因（响应照常返回、状态为失败），可改连接后再试。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
/// <param name="schemas">租户库的建库与建表。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsInitializeDatabase)]
public sealed class InitializeTenantDatabase(IRepository<SysTenant, Guid> tenants, TenantService service, TenantSchemas schemas, TimeProvider clock) : IOperation<InitializeTenantDatabaseRequest, TenantResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.initialize-database";

    /// <inheritdoc />
    public async ValueTask<Result<TenantResponse>> HandleAsync(InitializeTenantDatabaseRequest request, InvocationContext context)
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

        if (tenant.IsolationMode != TenantIsolationMode.Database)
        {
            return Error.Validation(TenancyErrors.NotIsolated, TenancyMessages.NotIsolated, MessageArguments.Of(("tenantId", tenant.TenantId)));
        }

        if (tenant.DatabaseStatus == TenantDatabaseStatus.Ready)
        {
            return Error.Conflict(TenancyErrors.DatabaseInitialized, TenancyMessages.DatabaseInitialized, MessageArguments.Of(("tenantId", tenant.TenantId)));
        }

        var initialized = await schemas.InitializeAsync(tenant.TenantId, context.CancellationToken).ConfigureAwait(false);
        var failure = initialized.IsFailure ? initialized.Error!.Message : null;
        tenant.DatabaseStatus = failure is null ? TenantDatabaseStatus.Ready : TenantDatabaseStatus.Failed;
        tenant.DatabaseError = failure is null ? null : failure[..Math.Min(failure.Length, 1024)];
        tenant.DatabaseInitializedAt = failure is null ? clock.GetUtcNow() : tenant.DatabaseInitializedAt;
        var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var synced = await service.SyncDirectoryAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        return synced.IsFailure ? synced.Error! : TenantResponse.From(tenant);
    }
}
