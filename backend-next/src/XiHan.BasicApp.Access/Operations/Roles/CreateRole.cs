// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 创建角色：<c>POST /api/roles</c>。角色码在作用侧内唯一。
/// </summary>
/// <param name="roles">角色仓储。</param>
/// <param name="service">角色域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesCreate)]
public sealed class CreateRole(IRepository<SysRole, Guid> roles, RoleService service, IIdGenerator<Guid> ids) : IOperation<CreateRoleRequest, RoleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.create";

    /// <inheritdoc />
    public async ValueTask<Result<RoleResponse>> HandleAsync(CreateRoleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var code = request.Code.Trim();
        if ((await service.FindByCodeAsync(owner, code, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(AccessErrors.RoleCodeTaken, AccessMessages.RoleCodeTaken, MessageArguments.Of(("code", code)));
        }

        var role = new SysRole
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            Code = code,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Order = request.Order,
        };
        var added = await roles.AddAsync(role, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : RoleResponse.From(role);
    }
}
