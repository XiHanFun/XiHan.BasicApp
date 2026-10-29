// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.OpenApiLogs;

/// <summary>
/// 开放接口日志详情。
/// </summary>
/// <remarks>
/// <c>GET /api/open-api-logs/{id}</c>。不在当前一侧即找不到。
/// </remarks>
/// <param name="queries">开放接口日志查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.OpenApiLogsRead)]
public sealed class GetOpenApiLog(OpenApiLogQueries queries) : IOperation<GetOpenApiLogRequest, OpenApiLogDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "open-api-logs.get";

    /// <inheritdoc />
    public ValueTask<Result<OpenApiLogDetailResponse>> HandleAsync(GetOpenApiLogRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
