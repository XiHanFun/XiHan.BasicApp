// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Operations.PermissionConditions;

/// <summary>
/// 条件属性目录：<c>GET /api/permission-conditions:attributes</c>。权限条件与访问策略共用，带每个属性可用的运算符与认证前能否取得。
/// </summary>
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
        var items = registry.Attributes
            .Select(static a => new ConditionAttributeResponse(a.Name, a.Kind, a.IsMultiValued, a.IsFamily, a.Aliases ?? [], ConditionOperatorRules.AllowedFor(a), AccessConditionAttributes.BeforeAuthentication.Contains(a.Name)))
            .ToList();
        return ValueTask.FromResult<Result<ConditionAttributesResponse>>(new ConditionAttributesResponse(items));
    }
}

/// <summary>
/// 一个主体上的授权绑定与条件：<c>GET /api/permission-conditions?subjectKind=&amp;subjectId=</c>。授予来自框架授予存储，拒绝来自规则行；子句含停用的。
/// </summary>
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
/// 在授权绑定上加一条条件子句：<c>POST /api/permission-conditions</c>。挂在授予上是满足才授予，挂在拒绝上是满足才拒绝；调用时按请求属性判定。
/// </summary>
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
/// 修改一条条件子句（含启停）：<c>PUT /api/permission-conditions/{id}</c>。启用时重新校验绑定。
/// </summary>
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
/// 删除条件子句：<c>DELETE /api/permission-conditions?ids=</c>。
/// </summary>
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
/// 权限条件的权限变更日志（ConditionChange）：目标是角色或用户，明细写动作、绑定与子句。
/// </summary>
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
