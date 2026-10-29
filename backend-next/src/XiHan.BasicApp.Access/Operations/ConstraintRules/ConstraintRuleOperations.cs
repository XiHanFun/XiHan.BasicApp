// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.ConstraintRules;

/// <summary>
/// 分页列出当前一侧的约束规则：<c>POST /api/constraint-rules:list</c>。缺省按求值顺序（优先级从大到小、再按编码）。
/// </summary>
/// <param name="queries">规则查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.ConstraintRulesRead)]
public sealed class ListConstraintRules(ConstraintRuleQueries queries) : IOperation<PageRequest, PageResult<ConstraintRuleResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "constraint-rules.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ConstraintRuleResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 按 ID 取当前一侧的约束规则：<c>GET /api/constraint-rules/{id}</c>。
/// </summary>
/// <param name="service">规则服务。</param>
/// <param name="projection">规则投影。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.ConstraintRulesRead)]
public sealed class GetConstraintRule(ConstraintRuleService service, ConstraintRuleProjection projection) : IOperation<GetConstraintRuleRequest, ConstraintRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "constraint-rules.get";

    /// <inheritdoc />
    public async ValueTask<Result<ConstraintRuleResponse>> HandleAsync(GetConstraintRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(AccessErrors.ConstraintRuleNotFound, AccessMessages.ConstraintRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return await projection.ToAsync(rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 在当前一侧新建约束规则：<c>POST /api/constraint-rules</c>。之后的指派、授予、继承、委托与会话激活按它判定（已有的不回头检查，4.x 同）。
/// </summary>
/// <param name="service">规则服务。</param>
/// <param name="projection">规则投影。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.ConstraintRulesCreate)]
public sealed class CreateConstraintRule(ConstraintRuleService service, ConstraintRuleProjection projection) : IOperation<CreateConstraintRuleRequest, ConstraintRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "constraint-rules.create";

    /// <inheritdoc />
    public async ValueTask<Result<ConstraintRuleResponse>> HandleAsync(CreateConstraintRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(RoleService.OwnerOf(context), request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var rule))
        {
            return created.Error!;
        }

        return await projection.ToAsync(rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 整体修改当前一侧的约束规则（含启停与规则项）：<c>PUT /api/constraint-rules/{id}</c>。
/// </summary>
/// <param name="service">规则服务。</param>
/// <param name="projection">规则投影。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.ConstraintRulesUpdate)]
public sealed class UpdateConstraintRule(ConstraintRuleService service, ConstraintRuleProjection projection) : IOperation<UpdateConstraintRuleRequest, ConstraintRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "constraint-rules.update";

    /// <inheritdoc />
    public async ValueTask<Result<ConstraintRuleResponse>> HandleAsync(UpdateConstraintRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(AccessErrors.ConstraintRuleNotFound, AccessMessages.ConstraintRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.UpdateAsync(rule, request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return await projection.ToAsync(rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 启用或停用约束规则：<c>POST /api/constraint-rules/{id}:set-status</c>。启用时重新校验规则项。
/// </summary>
/// <param name="service">规则服务。</param>
/// <param name="projection">规则投影。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.ConstraintRulesUpdate)]
public sealed class SetConstraintRuleStatus(ConstraintRuleService service, ConstraintRuleProjection projection) : IOperation<SetConstraintRuleStatusRequest, ConstraintRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "constraint-rules.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<ConstraintRuleResponse>> HandleAsync(SetConstraintRuleStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(AccessErrors.ConstraintRuleNotFound, AccessMessages.ConstraintRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.SetStatusAsync(rule, request.Status, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return await projection.ToAsync(rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量删除当前一侧的约束规则：<c>DELETE /api/constraint-rules?ids=...</c>，逐项结果。
/// </summary>
/// <param name="service">规则服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.ConstraintRulesDelete)]
public sealed class DeleteConstraintRules(ConstraintRuleService service) : IOperation<ConstraintRuleIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "constraint-rules.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(ConstraintRuleIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!(await service.FindAsync(id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
            {
                failed.Add(new BatchFailure(id, AccessErrors.ConstraintRuleNotFound));
                continue;
            }

            var removed = await service.DeleteAsync(rule, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
