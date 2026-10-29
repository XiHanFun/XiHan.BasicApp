// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.FieldSecurity;

/// <summary>
/// 列出字段安全的实体目录。
/// </summary>
/// <remarks>
/// <c>GET /api/field-security:entities</c>。各包登记的实体与可配置字段（字段名与接口字段同名）。
/// </remarks>
/// <param name="catalog">字段安全实体目录。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.FieldSecurityRead)]
public sealed class ListFieldSecurityEntities(FieldSecurityCatalog catalog) : IOperation<FieldSecurityEntitiesRequest, FieldSecurityEntitiesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "field-security.entities";

    /// <inheritdoc />
    public ValueTask<Result<FieldSecurityEntitiesResponse>> HandleAsync(FieldSecurityEntitiesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var items = catalog.Entities
            .Select(static e => new FieldSecurityEntityResponse(e.Code, e.TitleKey, e.Fields.Select(static f => new FieldSecurityFieldResponse(f.Name, f.TitleKey, f.IsText, f.IsNullable)).ToList()))
            .ToList();
        return ValueTask.FromResult<Result<FieldSecurityEntitiesResponse>>(new FieldSecurityEntitiesResponse(items));
    }
}

/// <summary>
/// 分页列出当前一侧的字段安全规则。
/// </summary>
/// <remarks>
/// <c>POST /api/field-security:list</c>。
/// </remarks>
/// <param name="queries">规则查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.FieldSecurityRead)]
public sealed class ListFieldSecurityRules(FieldSecurityQueries queries) : IOperation<PageRequest, PageResult<FieldSecurityRuleResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "field-security.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<FieldSecurityRuleResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 按 ID 取当前一侧的字段安全规则。
/// </summary>
/// <remarks>
/// <c>GET /api/field-security/{id}</c>。
/// </remarks>
/// <param name="service">字段安全规则服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.FieldSecurityRead)]
public sealed class GetFieldSecurityRule(FieldSecurityService service) : IOperation<GetFieldSecurityRuleRequest, FieldSecurityRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "field-security.get";

    /// <inheritdoc />
    public async ValueTask<Result<FieldSecurityRuleResponse>> HandleAsync(GetFieldSecurityRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(AccessErrors.FieldRuleNotFound, AccessMessages.FieldRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return await FieldSecurityLog.RespondAsync(service, rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 在当前一侧新建字段安全规则：<c>POST /api/field-security</c>。
/// </summary>
/// <param name="service">字段安全规则服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.FieldSecurityCreate)]
public sealed class CreateFieldSecurityRule(FieldSecurityService service) : IOperation<CreateFieldSecurityRuleRequest, FieldSecurityRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "field-security.create";

    /// <inheritdoc />
    public async ValueTask<Result<FieldSecurityRuleResponse>> HandleAsync(CreateFieldSecurityRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(RoleService.OwnerOf(context), request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var rule))
        {
            return created.Error!;
        }

        FieldSecurityLog.Record(context, rule, "create");
        return await FieldSecurityLog.RespondAsync(service, rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 整体修改当前一侧的字段安全规则（含启停）。
/// </summary>
/// <remarks>
/// <c>PUT /api/field-security/{id}</c>。
/// </remarks>
/// <param name="service">字段安全规则服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.FieldSecurityUpdate)]
public sealed class UpdateFieldSecurityRule(FieldSecurityService service) : IOperation<UpdateFieldSecurityRuleRequest, FieldSecurityRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "field-security.update";

    /// <inheritdoc />
    public async ValueTask<Result<FieldSecurityRuleResponse>> HandleAsync(UpdateFieldSecurityRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(AccessErrors.FieldRuleNotFound, AccessMessages.FieldRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var updated = await service.UpdateAsync(rule, request.ToDefinition(), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        FieldSecurityLog.Record(context, rule, "update");
        return await FieldSecurityLog.RespondAsync(service, rule, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量删除当前一侧的字段安全规则。
/// </summary>
/// <remarks>
/// <c>DELETE /api/field-security?ids=...</c>，逐项结果。
/// </remarks>
/// <param name="service">字段安全规则服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.FieldSecurityDelete)]
public sealed class DeleteFieldSecurityRules(FieldSecurityService service) : IOperation<FieldSecurityRuleIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "field-security.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(FieldSecurityRuleIdsRequest request, InvocationContext context)
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
                failed.Add(new BatchFailure(id, AccessErrors.FieldRuleNotFound));
                continue;
            }

            var removed = await service.DeleteAsync(rule, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            FieldSecurityLog.Record(context, rule, "delete");
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 字段安全规则操作的共同部分：登记权限变更日志、补目标显示名的响应。
/// </summary>
internal static class FieldSecurityLog
{
    /// <summary>
    /// 登记一条权限变更日志。
    /// </summary>
    /// <remarks>
    /// 角色目标记角色，用户目标记用户；细节写动作、实体字段与读取方式。
    /// </remarks>
    public static void Record(InvocationContext context, SysFieldSecurityRule rule, string action)
    {
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.FieldSecurity)
        {
            RoleId = rule.TargetKind == FieldSecurityTargetKind.Role ? rule.TargetId : null,
            TargetUserId = rule.TargetKind == FieldSecurityTargetKind.User ? rule.TargetId : null,
            Detail = $"{action} {rule.Entity}.{rule.Field} {rule.Strategy}{(rule.IsEditable ? " editable" : string.Empty)} {rule.TargetKind}:{rule.TargetId}",
        });
    }

    /// <summary>
    /// 带目标显示名的响应。
    /// </summary>
    public static async Task<FieldSecurityRuleResponse> RespondAsync(FieldSecurityService service, SysFieldSecurityRule rule, CancellationToken cancellationToken)
    {
        var names = await service.TargetNamesAsync([rule], cancellationToken).ConfigureAwait(false);
        return FieldSecurityRuleResponse.From(rule, names.GetValueOrDefault((rule.TargetKind, rule.TargetId)));
    }
}
