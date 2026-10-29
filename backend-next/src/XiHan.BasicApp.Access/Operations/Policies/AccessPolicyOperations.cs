// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.PermissionConditions;
using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Operations.Policies;

/// <summary>
/// 分页列出当前一侧的访问策略。
/// </summary>
/// <remarks>
/// <c>POST /api/policies:list</c>。缺省按求值顺序（优先级升序、再按编码）。
/// </remarks>
/// <param name="queries">策略查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesRead)]
public sealed class ListAccessPolicies(AccessPolicyQueries queries) : IOperation<PageRequest, PageResult<AccessPolicyResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<AccessPolicyResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 按 ID 取当前一侧的访问策略：<c>GET /api/policies/{id}</c>。
/// </summary>
/// <param name="service">策略服务。</param>
/// <param name="projection">策略投影。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesRead)]
public sealed class GetAccessPolicy(AccessPolicyService service, AccessPolicyProjection projection) : IOperation<GetAccessPolicyRequest, AccessPolicyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.get";

    /// <inheritdoc />
    public async ValueTask<Result<AccessPolicyResponse>> HandleAsync(GetAccessPolicyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var policy))
        {
            return Error.NotFound(AccessErrors.PolicyNotFound, AccessMessages.PolicyNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return await projection.ToAsync(policy, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 在当前一侧新建访问策略。
/// </summary>
/// <remarks>
/// <c>POST /api/policies</c>。新建一律只记录、启用；平台策略管全部调用，租户策略管本租户已登录的调用。
/// </remarks>
/// <param name="service">策略服务。</param>
/// <param name="projection">策略投影。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesCreate)]
public sealed class CreateAccessPolicy(AccessPolicyService service, AccessPolicyProjection projection) : IOperation<CreateAccessPolicyRequest, AccessPolicyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.create";

    /// <inheritdoc />
    public async ValueTask<Result<AccessPolicyResponse>> HandleAsync(CreateAccessPolicyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(RoleService.OwnerOf(context), request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var policy))
        {
            return created.Error!;
        }

        return await projection.ToAsync(policy, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 整体修改当前一侧的访问策略。
/// </summary>
/// <remarks>
/// <c>PUT /api/policies/{id}</c>。动作与状态不变；改完仍是启用中的拒绝策略、且会拦下这次请求时拒绝保存（防自锁）。
/// </remarks>
/// <param name="service">策略服务。</param>
/// <param name="projection">策略投影。</param>
/// <param name="security">安全配置（角色声明名）。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesUpdate)]
public sealed class UpdateAccessPolicy(AccessPolicyService service, AccessPolicyProjection projection, SecurityOptions security, TimeProvider clock) : IOperation<UpdateAccessPolicyRequest, AccessPolicyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.update";

    /// <inheritdoc />
    public async ValueTask<Result<AccessPolicyResponse>> HandleAsync(UpdateAccessPolicyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var policy))
        {
            return Error.NotFound(AccessErrors.PolicyNotFound, AccessMessages.PolicyNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.UpdateAsync(policy, request.ToDefinition(), AccessRequests.Current(context, security, clock.GetUtcNow()), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return await projection.ToAsync(policy, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 改访问策略的动作。
/// </summary>
/// <remarks>
/// <c>POST /api/policies/{id}:set-action</c>。改成拒绝且策略启用中时，会拦下这次请求即拒绝保存（防自锁）。
/// </remarks>
/// <param name="service">策略服务。</param>
/// <param name="projection">策略投影。</param>
/// <param name="security">安全配置（角色声明名）。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesUpdate)]
public sealed class SetAccessPolicyAction(AccessPolicyService service, AccessPolicyProjection projection, SecurityOptions security, TimeProvider clock) : IOperation<SetAccessPolicyActionRequest, AccessPolicyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.set-action";

    /// <inheritdoc />
    public async ValueTask<Result<AccessPolicyResponse>> HandleAsync(SetAccessPolicyActionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var policy))
        {
            return Error.NotFound(AccessErrors.PolicyNotFound, AccessMessages.PolicyNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.SetActionAsync(policy, request.Action, AccessRequests.Current(context, security, clock.GetUtcNow()), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return await projection.ToAsync(policy, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 启用或停用访问策略。
/// </summary>
/// <remarks>
/// <c>POST /api/policies/{id}:set-status</c>。启用一条拒绝策略时，会拦下这次请求即拒绝保存（防自锁）。
/// </remarks>
/// <param name="service">策略服务。</param>
/// <param name="projection">策略投影。</param>
/// <param name="security">安全配置（角色声明名）。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesUpdate)]
public sealed class SetAccessPolicyStatus(AccessPolicyService service, AccessPolicyProjection projection, SecurityOptions security, TimeProvider clock) : IOperation<SetAccessPolicyStatusRequest, AccessPolicyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<AccessPolicyResponse>> HandleAsync(SetAccessPolicyStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var policy))
        {
            return Error.NotFound(AccessErrors.PolicyNotFound, AccessMessages.PolicyNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.SetStatusAsync(policy, request.Status, AccessRequests.Current(context, security, clock.GetUtcNow()), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return await projection.ToAsync(policy, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量删除当前一侧的访问策略。
/// </summary>
/// <remarks>
/// <c>DELETE /api/policies?ids=...</c>，逐项结果。
/// </remarks>
/// <param name="service">策略服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PoliciesDelete)]
public sealed class DeleteAccessPolicies(AccessPolicyService service) : IOperation<AccessPolicyIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(AccessPolicyIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!(await service.FindAsync(id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var policy))
            {
                failed.Add(new BatchFailure(id, AccessErrors.PolicyNotFound));
                continue;
            }

            var removed = await service.DeleteAsync(policy, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 列出访问策略可用的条件属性。
/// </summary>
/// <remarks>
/// <c>GET /api/policies:attributes</c>。与 <c>permission-conditions.attributes</c> 同一份目录，按访问策略的权限给出（4.x 同）。
/// </remarks>
/// <param name="registry">条件属性登记表。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PoliciesRead)]
public sealed class ListAccessPolicyAttributes(IConditionAttributeRegistry registry) : IOperation<AccessPolicyAttributesRequest, ConditionAttributesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.attributes";

    /// <inheritdoc />
    public ValueTask<Result<ConditionAttributesResponse>> HandleAsync(AccessPolicyAttributesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.FromResult<Result<ConditionAttributesResponse>>(ListConditionAttributes.Of(registry));
    }
}

/// <summary>
/// 列出访问策略的预置模板。
/// </summary>
/// <remarks>
/// <c>GET /api/policies:templates</c>。页面上从模板新建用（新建出来一律只记录），时机按当前一侧推出。
/// </remarks>
/// <param name="service">策略服务（编译）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PoliciesRead)]
public sealed class ListAccessPolicyTemplates(AccessPolicyService service) : IOperation<AccessPolicyTemplatesRequest, AccessPolicyTemplatesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.templates";

    /// <inheritdoc />
    public ValueTask<Result<AccessPolicyTemplatesResponse>> HandleAsync(AccessPolicyTemplatesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var platform = string.Equals(RoleService.OwnerOf(context), TenantIds.Platform, StringComparison.Ordinal);
        var items = AccessPolicyTemplates.All.Select(t => new AccessPolicyTemplateResponse(
            t.Code,
            t.Name,
            t.Description,
            t.Action,
            t.Status,
            t.Priority,
            service.Compile(t.Conditions).TryGetValue(out var compiled) ? AccessPolicyEvaluator.PhaseOf(compiled, platform) : ConditionHitPhase.AfterAuthentication,
            [.. t.Conditions.Select(AccessPolicyConditionResponse.From)])).ToList();
        return ValueTask.FromResult<Result<AccessPolicyTemplatesResponse>>(new AccessPolicyTemplatesResponse(items));
    }
}

/// <summary>
/// 试运行访问策略。
/// </summary>
/// <remarks>
/// <c>POST /api/policies:trial</c>（带草稿条件，走 POST）。已有策略或草稿条件按给定的请求情形只求这一条、不看有效期，逐组逐条件说明。
/// </remarks>
/// <param name="trials">试运行。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PoliciesTrial)]
public sealed class TrialAccessPolicy(AccessPolicyTrialService trials) : IOperation<TrialAccessPolicyRequest, AccessPolicyTrialResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "policies.trial";

    /// <inheritdoc />
    public async ValueTask<Result<AccessPolicyTrialResponse>> HandleAsync(TrialAccessPolicyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await trials.TrialAsync(RoleService.OwnerOf(context), request, e => context.Localize(e), context.CancellationToken).ConfigureAwait(false);
    }
}
