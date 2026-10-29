// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Operations.PermissionConditions;

/// <summary>
/// 列出条件属性目录。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-conditions:attributes</c>。权限条件与访问策略共用，带每个属性可用的运算符与认证前能否取得。
/// </remarks>
/// <param name="registry">条件属性登记表。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsRead)]
public sealed class ListConditionAttributes(IConditionAttributeRegistry registry) : IOperation<ConditionAttributesRequest, ConditionAttributesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.attributes";

    /// <inheritdoc />
    public ValueTask<Result<ConditionAttributesResponse>> HandleAsync(ConditionAttributesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.FromResult<Result<ConditionAttributesResponse>>(Of(registry));
    }

    /// <summary>
    /// 登记表投影成目录（访问策略的 <c>policies.attributes</c> 共用）。
    /// </summary>
    public static ConditionAttributesResponse Of(IConditionAttributeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return new ConditionAttributesResponse(registry.Attributes
            .Select(static a => new ConditionAttributeResponse(a.Name, a.Kind, a.IsMultiValued, a.IsFamily, a.Aliases ?? [], ConditionOperatorRules.AllowedFor(a), AccessConditionAttributes.BeforeAuthentication.Contains(a.Name)))
            .ToList());
    }
}

/// <summary>
/// 列出一个主体上的授权绑定与条件。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-conditions?subjectKind=&amp;subjectId=</c>。授予来自框架授予存储，拒绝来自规则行；子句含停用的。
/// </remarks>
/// <param name="service">条件服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsRead)]
public sealed class ListPermissionConditions(PermissionConditionService service) : IOperation<PermissionConditionsRequest, PermissionConditionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.list";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionConditionsResponse>> HandleAsync(PermissionConditionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var bindings = await service.BindingsAsync(owner, request.SubjectKind, request.SubjectId, context.CancellationToken).ConfigureAwait(false);
        if (!bindings.TryGetValue(out var current))
        {
            return bindings.Error!;
        }

        var rows = await service.ListAsync(owner, request.SubjectKind, request.SubjectId, context.CancellationToken).ConfigureAwait(false);
        return new PermissionConditionsResponse(
            request.SubjectKind,
            request.SubjectId,
            [.. current.OrderBy(static b => b.Permission, StringComparer.Ordinal).ThenBy(static b => b.Action).Select(static b => new ConditionBindingResponse(b.Permission, b.Action, b.EffectiveAt, b.ExpiresAt))],
            [.. rows.OrderBy(static c => c.Permission, StringComparer.Ordinal).ThenBy(static c => c.Action).ThenBy(static c => c.GroupNo).ThenBy(static c => c.CreatedAt).Select(PermissionConditionResponse.From)]);
    }
}

/// <summary>
/// 在授权绑定上加一条条件子句。
/// </summary>
/// <remarks>
/// <c>POST /api/permission-conditions</c>。挂在授予上是满足才授予，挂在拒绝上是满足才拒绝；调用时按请求属性判定。
/// </remarks>
/// <param name="service">条件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsCreate)]
public sealed class CreatePermissionCondition(PermissionConditionService service) : IOperation<CreatePermissionConditionRequest, PermissionConditionResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.create";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionConditionResponse>> HandleAsync(CreatePermissionConditionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var binding = new ConditionBinding(request.SubjectKind, request.SubjectId, request.Permission.Trim(), request.Action);
        var created = await service.CreateAsync(RoleService.OwnerOf(context), binding, request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var condition))
        {
            return created.Error!;
        }

        ConditionLog.Record(context, condition, "create");
        return PermissionConditionResponse.From(condition);
    }
}

/// <summary>
/// 修改一条条件子句（含启停）。
/// </summary>
/// <remarks>
/// <c>PUT /api/permission-conditions/{id}</c>。启用时重新校验绑定。
/// </remarks>
/// <param name="service">条件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsUpdate)]
public sealed class UpdatePermissionCondition(PermissionConditionService service) : IOperation<UpdatePermissionConditionRequest, PermissionConditionResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.update";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionConditionResponse>> HandleAsync(UpdatePermissionConditionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var condition))
        {
            return Error.NotFound(AccessErrors.ConditionNotFound, AccessMessages.ConditionNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.UpdateAsync(condition, request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        ConditionLog.Record(context, condition, "update");
        return PermissionConditionResponse.From(condition);
    }
}

/// <summary>
/// 删除条件子句。
/// </summary>
/// <remarks>
/// <c>DELETE /api/permission-conditions?ids=</c>。
/// </remarks>
/// <param name="service">条件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsDelete)]
public sealed class DeletePermissionConditions(PermissionConditionService service) : IOperation<PermissionConditionIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(PermissionConditionIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!(await service.FindAsync(id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var condition))
            {
                failed.Add(new BatchFailure(id, AccessErrors.ConditionNotFound));
                continue;
            }

            var removed = await service.DeleteAsync(condition, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            ConditionLog.Record(context, condition, "delete");
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 权限条件的权限变更日志（ConditionChange）。
/// </summary>
/// <remarks>
/// 目标是角色或用户，明细写动作、绑定与子句。
/// </remarks>
internal static class ConditionLog
{
    /// <summary>
    /// 记一条。
    /// </summary>
    public static void Record(InvocationContext context, SysPermissionCondition condition, string action)
    {
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.ConditionChange)
        {
            RoleId = condition.SubjectKind == GrantSubjectKind.Role ? condition.SubjectId : null,
            TargetUserId = condition.SubjectKind == GrantSubjectKind.User ? condition.SubjectId : null,
            PermissionCodes = [condition.Permission],
            Detail = $"{action} {condition.Action} group:{condition.GroupNo} {(condition.IsNegated ? "not " : string.Empty)}{condition.AttributeName} {condition.Operator} {condition.Value} {condition.Status}",
        });
    }
}

/// <summary>
/// 条件试算。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-conditions:trial</c>。一个用户在一个权限码上按给定的请求情形判定，逐组逐子句说明；按他此刻的有效指派与委托，不看令牌。
/// </remarks>
/// <param name="insight">试算与盘点。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsTrial)]
public sealed class TrialPermissionCondition(ConditionInsightService insight) : IOperation<ConditionTrialRequest, ConditionTrialResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.trial";

    /// <inheritdoc />
    public async ValueTask<Result<ConditionTrialResponse>> HandleAsync(ConditionTrialRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await insight.TrialAsync(RoleService.OwnerOf(context), request, e => context.Localize(e), context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 影响试算。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-conditions:batch-trial</c>。一个绑定影响到的人此刻各自的结论（角色即它及其下级角色的有效成员，最多 200 人）。
/// </remarks>
/// <param name="insight">试算与盘点。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsTrial)]
public sealed class BatchTrialPermissionCondition(ConditionInsightService insight) : IOperation<ConditionBatchTrialRequest, ConditionBatchTrialResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.batch-trial";

    /// <inheritdoc />
    public async ValueTask<Result<ConditionBatchTrialResponse>> HandleAsync(ConditionBatchTrialRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await insight.BatchTrialAsync(RoleService.OwnerOf(context), request, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 条件盘点。
/// </summary>
/// <remarks>
/// <c>GET /api/permission-conditions:inventory</c>。这一侧每条子句的诊断（属性未登记、旧写法、编译不过、绑定已不在）与所在组的结论。
/// </remarks>
/// <param name="insight">试算与盘点。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.PermissionConditionsTrial)]
public sealed class PermissionConditionInventory(ConditionInsightService insight) : IOperation<ConditionInventoryRequest, ConditionInventoryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permission-conditions.inventory";

    /// <inheritdoc />
    public async ValueTask<Result<ConditionInventoryResponse>> HandleAsync(ConditionInventoryRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await insight.InventoryAsync(RoleService.OwnerOf(context), e => context.Localize(e), context.CancellationToken).ConfigureAwait(false);
    }
}
