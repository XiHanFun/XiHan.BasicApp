// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.BasicApp.Maintenance.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Versions;

/// <summary>
/// 分页列出版本台账。
/// </summary>
/// <remarks>
/// <c>POST /api/versions:list</c>。每个库一行（目前只有平台库），由升级执行器维护，只读。关键字匹配应用版本、结构版本与升级节点；
/// 筛选支持 <c>isUpgrading</c>、<c>dbVersion</c>、<c>appVersion</c>（<c>eq</c>），排序支持 <c>appVersion</c>、<c>dbVersion</c>、<c>upgradedAt</c>。只在平台一侧。
/// </remarks>
/// <param name="queries">台账与执行记录的查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsRead)]
public sealed class ListVersions(SchemaQueries queries) : IOperation<PageRequest, PageResult<SchemaVersionResponse>>
{
    /// <summary>
    /// 操作 ID。
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
/// 取一条版本台账。
/// </summary>
/// <remarks>
/// <c>GET /api/versions/{id}</c>。只在平台一侧。
/// </remarks>
/// <param name="queries">台账与执行记录的查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.VersionsRead)]
public sealed class GetVersion(SchemaQueries queries) : IOperation<VersionIdRequest, SchemaVersionResponse>
{
    /// <summary>
    /// 操作 ID。
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
