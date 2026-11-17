// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.BasicApp.Maintenance.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Migrations;

/// <summary>
/// 分页列出一个库的升级执行记录
/// </summary>
/// <remarks>
/// <c>POST /api/migrations:list</c>。筛选 <c>database</c>（<c>eq</c>）选库：不给为平台库，独立库为租户 ID（记录在各自的库里）。缺省按执行时间倒序。关键字匹配版本、脚本名与节点；
/// 筛选另支持 <c>runId</c>、<c>version</c>（<c>eq</c> / <c>contains</c>）、<c>scriptName</c>、<c>nodeName</c>（<c>contains</c>）、<c>success</c>（<c>eq</c>）、<c>executedAt</c>（<c>ge</c> / <c>lt</c>）；
/// 排序支持 <c>executedAt</c>、<c>version</c>、<c>scriptName</c>、<c>durationMs</c>。列表不带执行的 SQL（<see cref="MigrationResponse.HasSql"/> 标出有没有），取单条时带。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
[Operation(Id, Expose = Expose.Http, Permission = MaintenancePermissions.MigrationsRead)]
public sealed class ListMigrations(SchemaQueries queries) : IOperation<PageRequest, PageResult<MigrationResponse>>
{
    /// <summary>
    /// 操作 ID
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
/// 取一条执行记录
/// </summary>
/// <remarks>
/// <c>GET /api/migrations/{id}</c>，<c>database</c> 指定记录所在的库（不给为平台库）。带执行的 SQL。只在平台一侧
/// </remarks>
/// <param name="queries">台账与执行记录的查询</param>
[Operation(Id, Expose = Expose.Http, Permission = MaintenancePermissions.MigrationsRead)]
public sealed class GetMigration(SchemaQueries queries) : IOperation<MigrationIdRequest, MigrationDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "migrations.get";

    /// <inheritdoc />
    public async ValueTask<Result<MigrationDetailResponse>> HandleAsync(MigrationIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobViews.RequirePlatform(context) is { } denied ? denied : await queries.GetMigrationAsync(request.Id, request.Database, context).ConfigureAwait(false);
    }
}
