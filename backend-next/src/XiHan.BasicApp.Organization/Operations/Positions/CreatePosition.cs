// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Positions;

/// <summary>
/// 创建岗位：<c>POST /api/positions</c>。编码租户内唯一。
/// </summary>
/// <param name="positions">岗位仓储。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.PositionsCreate)]
public sealed class CreatePosition(IRepository<SysPosition, Guid> positions, IIdGenerator<Guid> ids) : IOperation<CreatePositionRequest, PositionResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "positions.create";

    /// <inheritdoc />
    public async ValueTask<Result<PositionResponse>> HandleAsync(CreatePositionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var code = request.Code.Trim();
        if (await positions.CountAsync(EntityQuery.All<SysPosition>().Where(new Specification<SysPosition>(p => p.Code == code)), context.CancellationToken).ConfigureAwait(false) > 0)
        {
            return Error.Conflict(OrganizationErrors.PositionCodeTaken, OrganizationMessages.PositionCodeTaken, MessageArguments.Of(("code", code)));
        }

        var position = new SysPosition
        {
            Id = ids.NewId(),
            Code = code,
            Name = request.Name.Trim(),
            Order = request.Order,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        var added = await positions.AddAsync(position, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : PositionResponse.From(position);
    }
}
