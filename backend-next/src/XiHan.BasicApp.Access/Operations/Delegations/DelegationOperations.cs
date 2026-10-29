// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Delegations;

/// <summary>
/// 分页列出当前一侧的委托：<c>POST /api/delegations:list</c>。状态按撤销时间与有效期推出（未到生效时间、生效中、已到期、已撤销）。
/// </summary>
/// <param name="queries">委托查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.DelegationsRead)]
public sealed class ListDelegations(DelegationQueries queries) : IOperation<PageRequest, PageResult<DelegationResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "delegations.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<DelegationResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 新建委托：<c>POST /api/delegations:create</c>。委托人此刻须持有所委托的权限码或角色；被委托人在有效期内按无条件授予拿到权限码，或按角色的继承链拿到授予（不拿数据范围与字段安全）。
/// </summary>
/// <param name="service">委托服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.DelegationsCreate)]
public sealed class CreateDelegation(DelegationService service) : IOperation<CreateDelegationRequest, DelegationResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "delegations.create";

    /// <inheritdoc />
    public async ValueTask<Result<DelegationResponse>> HandleAsync(CreateDelegationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(RoleService.OwnerOf(context), request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var delegation))
        {
            return created.Error!;
        }

        DelegationLog.Record(context, delegation, "create");
        return await DelegationLog.RespondAsync(service, delegation, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 撤销委托：<c>POST /api/delegations:revoke</c>。只撤销未到生效时间或生效中的，保留记录；撤销即刻失效。
/// </summary>
/// <param name="service">委托服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.DelegationsRevoke)]
public sealed class RevokeDelegation(DelegationService service) : IOperation<RevokeDelegationRequest, DelegationResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "delegations.revoke";

    /// <inheritdoc />
    public async ValueTask<Result<DelegationResponse>> HandleAsync(RevokeDelegationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var delegation))
        {
            return Error.NotFound(AccessErrors.DelegationNotFound, AccessMessages.DelegationNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var revokedBy = Guid.TryParse(context.GetSecurity().Principal.Id, out var actor) ? actor : (Guid?)null;
        var revoked = await service.RevokeAsync(delegation, revokedBy, context.CancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
        {
            return revoked.Error!;
        }

        DelegationLog.Record(context, delegation, "revoke");
        return await DelegationLog.RespondAsync(service, delegation, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 删除委托记录：<c>POST /api/delegations:delete</c>。任何状态都可删；生效中的随之失效。
/// </summary>
/// <param name="service">委托服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.DelegationsDelete)]
public sealed class DeleteDelegations(DelegationService service) : IOperation<DelegationIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "delegations.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(DelegationIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!(await service.FindAsync(id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var delegation))
            {
                failed.Add(new BatchFailure(id, AccessErrors.DelegationNotFound));
                continue;
            }

            var removed = await service.DeleteAsync(delegation, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            DelegationLog.Record(context, delegation, "delete");
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 委托的权限变更日志与响应组装。
/// </summary>
internal static class DelegationLog
{
    /// <summary>
    /// 记一条权限变更日志（Delegation）：目标用户是被委托人，明细写动作、委托人与目标。
    /// </summary>
    public static void Record(InvocationContext context, SysPermissionDelegation delegation, string action)
    {
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.Delegation)
        {
            RoleId = delegation.RoleId,
            TargetUserId = delegation.DelegateeId,
            PermissionCodes = delegation.Permission is { } permission ? [permission] : [],
            Detail = $"{action} from:{delegation.DelegatorId} {(delegation.Permission is { } code ? code : $"role:{delegation.RoleId}")} until:{delegation.ExpiresAt:O}",
        });
    }

    /// <summary>
    /// 带显示名的响应。
    /// </summary>
    public static async Task<DelegationResponse> RespondAsync(DelegationService service, SysPermissionDelegation delegation, InvocationContext context)
    {
        var (users, roles) = await service.NamesAsync([delegation], delegation.OwnerTenantId, context.CancellationToken).ConfigureAwait(false);
        return DelegationResponse.From(delegation, service.Now, users, roles);
    }
}
