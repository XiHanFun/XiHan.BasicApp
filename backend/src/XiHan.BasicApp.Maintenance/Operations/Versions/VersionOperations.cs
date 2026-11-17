// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.BasicApp.Maintenance.Queries;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Versions;

/// <summary>
/// 分页列出版本台账
/// </summary>
/// <remarks>
/// <c>POST /api/versions:list</c>。每个库一行：平台库与已就绪的租户独立库（台账在各自的库里，读不到的那行状态为不可达并带原因），由升级执行器维护
/// 关键字匹配库名、应用版本、结构版本与升级节点；筛选支持 <c>database</c>、<c>isUpgrading</c>、<c>dbVersion</c>、<c>appVersion</c>、<c>provider</c>、<c>status</c>（<c>eq</c>），
/// 排序支持 <c>database</c>、<c>appVersion</c>、<c>dbVersion</c>、<c>upgradedAt</c>、<c>status</c>（缺省平台库在前、其余按库名）。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
[Operation(Id, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsRead)]
public sealed class ListVersions(SchemaQueries queries) : IOperation<PageRequest, PageResult<SchemaVersionResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "versions.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<SchemaVersionResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobViews.RequirePlatform(context) is { } denied ? denied : await queries.ListVersionsAsync(request, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 取一个库的版本台账
/// </summary>
/// <remarks>
/// <c>GET /api/versions/{id}</c>，<c>id</c> 是库名（<c>platform</c> 或独立库的租户 ID）。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
[Operation(Id, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsRead)]
public sealed class GetVersion(SchemaQueries queries) : IOperation<VersionIdRequest, SchemaVersionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "versions.get";

    /// <inheritdoc />
    public async ValueTask<Result<SchemaVersionResponse>> HandleAsync(VersionIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobViews.RequirePlatform(context) is { } denied ? denied : await queries.GetVersionAsync(request.Id, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 预览一个库的结构升级
/// </summary>
/// <remarks>
/// <c>GET /api/versions/{id}:plan</c>，<c>id</c> 是库名。只读：列出升到当前应用版本要执行的脚本（带原文）、要建的表、要加的列与要建的索引，不执行、不抢锁。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
/// <param name="upgrader">结构升级执行器</param>
[Operation(Id, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsRead)]
public sealed class PlanVersion(SchemaQueries queries, SchemaUpgrader upgrader) : IOperation<VersionIdRequest, SchemaPlanResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "versions.plan";

    /// <inheritdoc />
    public async ValueTask<Result<SchemaPlanResponse>> HandleAsync(VersionIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var known = await VersionGuards.RequireKnownAsync(queries, request.Id, context).ConfigureAwait(false);
        if (known.IsFailure)
        {
            return known.Error!;
        }

        var planned = await upgrader.PlanAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return planned.TryGetValue(out var plan) ? SchemaPlanResponse.Of(plan) : planned.Error!;
    }
}

/// <summary>
/// 重试升级一个租户独立库
/// </summary>
/// <remarks>
/// <c>POST /api/versions/{id}:upgrade</c>，<c>id</c> 是租户 ID。执行这个库比台账新的脚本、按实体补表补列：只锁这个库自己的台账行，别的调用与别的节点不受影响；有节点正在升级它即 <c>RUNTIME_UNAVAILABLE_002</c>，不等
/// 成功后这个租户恢复可用（别的节点在一分钟内复查后放行）；失败照样记进这个库的升级记录与台账。平台库在节点启动时升级，指定平台库即 <see cref="MaintenanceErrors.PlatformUpgradeAtStartup"/>。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
/// <param name="upgrader">结构升级执行器</param>
[Operation(Id, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsUpgrade)]
public sealed class UpgradeVersion(SchemaQueries queries, SchemaUpgrader upgrader) : IOperation<VersionIdRequest, SchemaUpgradeResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "versions.upgrade";

    /// <inheritdoc />
    public async ValueTask<Result<SchemaUpgradeResponse>> HandleAsync(VersionIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (string.Equals(request.Id, SchemaVersionResponse.Platform, StringComparison.Ordinal))
        {
            return Error.Validation(MaintenanceErrors.PlatformUpgradeAtStartup, MaintenanceMessages.PlatformUpgradeAtStartup);
        }

        var known = await VersionGuards.RequireKnownAsync(queries, request.Id, context).ConfigureAwait(false);
        if (known.IsFailure)
        {
            return known.Error!;
        }

        var upgraded = await upgrader.UpgradeTenantAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return upgraded.TryGetValue(out var report)
            ? new SchemaUpgradeResponse(request.Id, report.RunId, report.From, report.To, report.Applied)
            : upgraded.Error!;
    }
}

/// <summary>
/// 手动释放升级锁
/// </summary>
/// <remarks>
/// <c>POST /api/versions/{id}:release-lock</c>，<c>id</c> 是库名。持锁的节点已经不在、又不想等锁过期时用：锁还在 <c>node</c> 手里才释放，否则 <see cref="MaintenanceErrors.SchemaLockNotHeld"/>，不会抢走别的节点刚拿到的锁
/// 释放正在升级的节点的锁会让两个节点同时升级同一个库：确认那个节点已经退出再释放。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
/// <param name="upgrader">结构升级执行器</param>
[Operation(Id, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsReleaseLock)]
public sealed class ReleaseVersionLock(SchemaQueries queries, SchemaUpgrader upgrader) : IOperation<ReleaseVersionLockRequest, SchemaVersionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "versions.release-lock";

    /// <inheritdoc />
    public async ValueTask<Result<SchemaVersionResponse>> HandleAsync(ReleaseVersionLockRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var known = await VersionGuards.RequireKnownAsync(queries, request.Id, context).ConfigureAwait(false);
        if (known.IsFailure)
        {
            return known.Error!;
        }

        var released = await upgrader.ReleaseLockAsync(request.Id, request.Node, context.CancellationToken).ConfigureAwait(false);
        if (!released.TryGetValue(out var done))
        {
            return released.Error!;
        }

        return done
            ? await queries.GetVersionAsync(request.Id, context).ConfigureAwait(false)
            : Error.Conflict(MaintenanceErrors.SchemaLockNotHeld, MaintenanceMessages.SchemaLockNotHeld, MessageArguments.Of(("id", request.Id), ("node", request.Node)));
    }
}
