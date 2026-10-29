// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Positions;

/// <summary>
/// 修改岗位。
/// </summary>
/// <remarks>
/// <c>PUT /api/positions/{id}</c>（名称、排序、说明、状态）。
/// </remarks>
/// <param name="positions">岗位仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.PositionsUpdate)]
public sealed class UpdatePosition(IRepository<SysPosition, Guid> positions) : IOperation<UpdatePositionRequest, PositionResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "positions.update";

    /// <inheritdoc />
    public async ValueTask<Result<PositionResponse>> HandleAsync(UpdatePositionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        if (!(await positions.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var position))
        {
            return Error.NotFound(OrganizationErrors.PositionNotFound, OrganizationMessages.PositionNotFound, MessageArguments.Of(("id", request.Id)));
        }

        position.Name = request.Name.Trim();
        position.Order = request.Order;
        position.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        position.Status = request.Status;
        var updated = await positions.UpdateAsync(position, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : PositionResponse.From(position);
    }
}
