// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Positions;

/// <summary>
/// 岗位详情。
/// </summary>
/// <remarks>
/// <c>GET /api/positions/{id}</c>。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </remarks>
/// <param name="positions">岗位仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = OrganizationPermissions.PositionsRead)]
public sealed class GetPosition(IRepository<SysPosition, Guid> positions) : IOperation<GetPositionRequest, PositionResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "positions.get";

    /// <inheritdoc />
    public async ValueTask<Result<PositionResponse>> HandleAsync(GetPositionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        return (await positions.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var position)
            ? PositionResponse.From(position)
            : Error.NotFound(OrganizationErrors.PositionNotFound, OrganizationMessages.PositionNotFound, MessageArguments.Of(("id", request.Id)));
    }
}
