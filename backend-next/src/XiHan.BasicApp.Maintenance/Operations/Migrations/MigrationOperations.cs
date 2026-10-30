// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.BasicApp.Maintenance.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Migrations;

/// <summary>
/// 分页列出升级脚本的执行记录。
/// </summary>
/// <remarks>
/// <c>POST /api/migrations:list</c>。缺省按执行时间倒序。关键字匹配版本、脚本名与节点；筛选支持 <c>version</c>（<c>eq</c> / <c>contains</c>）、
/// <c>scriptName</c>、<c>nodeName</c>（<c>contains</c>）、<c>success</c>（<c>eq</c>）、<c>executedAt</c>（<c>ge</c> / <c>lt</c>）；排序支持 <c>executedAt</c>、<c>version</c>、<c>scriptName</c>、<c>durationMs</c>。只在平台一侧。
/// </remarks>
/// <param name="queries">台账与执行记录的查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.MigrationsRead)]
public sealed class ListMigrations(SchemaQueries queries) : IOperation<PageRequest, PageResult<MigrationResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "migrations.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<MigrationResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobViews.RequirePlatform(context) is { } denied ? denied : await queries.ListMigrationsAsync(request, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 取一条执行记录。
/// </summary>
/// <remarks>
/// <c>GET /api/migrations/{id}</c>。只在平台一侧。
/// </remarks>
/// <param name="queries">台账与执行记录的查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.MigrationsRead)]
public sealed class GetMigration(SchemaQueries queries) : IOperation<MigrationIdRequest, MigrationResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "migrations.get";

    /// <inheritdoc />
    public async ValueTask<Result<MigrationResponse>> HandleAsync(MigrationIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobViews.RequirePlatform(context) is { } denied ? denied : await queries.GetMigrationAsync(request.Id, context).ConfigureAwait(false);
    }
}
