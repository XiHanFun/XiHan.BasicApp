// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.BasicApp.Workflow.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Serialization;

namespace XiHan.BasicApp.Workflow.Operations.WorkflowDefinitions;

/// <summary>
/// 流程定义列表。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-definitions:list</c>。当前一侧的定义（平台只看平台的，租户只看本租户的），框架在存储里按关键字（名字、显示名、说明）、名字、分类与状态过滤并分页，
/// 名字升序、版本降序；筛选见 <see cref="WorkflowQueryMapping.Definitions"/>。
/// </remarks>
/// <param name="catalog">定义目录。</param>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsRead)]
public sealed class ListWorkflowDefinitions(IWorkflowDefinitionCatalog catalog, WorkflowBusinessRegistry business) : IOperation<PageRequest, PageResult<WorkflowDefinitionResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<WorkflowDefinitionResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = WorkflowQueryMapping.Definitions(request);
        if (!query.TryGetValue(out var definitionQuery))
        {
            return query.Error!;
        }

        var page = await catalog.QueryAsync(definitionQuery, context.CancellationToken).ConfigureAwait(false);
        return page.TryGetValue(out var found)
            ? new PageResult<WorkflowDefinitionResponse>(found.Items.Select(s => WorkflowDefinitionResponse.From(s, business)).ToList(), found.Total, request.Page)
            : page.Error!;
    }
}

/// <summary>
/// 流程定义详情。
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-definitions/{id}</c>。带整份定义（WorkflowJson），设计器按它打开；别的分区的当作不存在。
/// </remarks>
/// <param name="catalog">定义目录。</param>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsRead)]
public sealed class GetWorkflowDefinition(IWorkflowDefinitionCatalog catalog, WorkflowBusinessRegistry business) : IOperation<GetWorkflowDefinitionRequest, WorkflowDefinitionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.get";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowDefinitionDetailResponse>> HandleAsync(GetWorkflowDefinitionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await WorkflowDefinitions.FindAsync(catalog, request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var definition) ? WorkflowDefinitionDetailResponse.From(definition, business) : found.Error!;
    }
}

/// <summary>
/// 新建流程定义草稿。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-definitions</c>。文档按框架的 WorkflowJson 读（不合法即 <c>WORKFLOW_VALIDATION_001</c>），ID 由框架分配，版本按同名最大版本加一；建草稿不做结构校验，发布时才校验。
/// </remarks>
/// <param name="catalog">定义目录。</param>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsCreate)]
public sealed class CreateWorkflowDefinition(IWorkflowDefinitionCatalog catalog, WorkflowBusinessRegistry business) : IOperation<CreateWorkflowDefinitionRequest, WorkflowDefinitionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.create";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowDefinitionDetailResponse>> HandleAsync(CreateWorkflowDefinitionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var parsed = WorkflowDefinitions.Parse(request.Document, WorkflowIds.ToEngine(Guid.CreateVersion7()));
        if (!parsed.TryGetValue(out var definition))
        {
            return parsed.Error!;
        }

        var created = await catalog.CreateAsync(definition, context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var draft) ? WorkflowDefinitionDetailResponse.From(draft, business) : created.Error!;
    }
}

/// <summary>
/// 改流程定义草稿。
/// </summary>
/// <remarks>
/// <c>PUT /api/workflow-definitions/{id}</c>。只有草稿能改（别的状态是框架的 <c>WORKFLOW_CONFLICT_002</c>）；名字与版本沿用已存的。
/// </remarks>
/// <param name="catalog">定义目录。</param>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsUpdate)]
public sealed class UpdateWorkflowDefinition(IWorkflowDefinitionCatalog catalog, WorkflowBusinessRegistry business) : IOperation<UpdateWorkflowDefinitionRequest, WorkflowDefinitionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.update";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowDefinitionDetailResponse>> HandleAsync(UpdateWorkflowDefinitionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await WorkflowDefinitions.FindAsync(catalog, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var stored))
        {
            return found.Error!;
        }

        var parsed = WorkflowDefinitions.Parse(request.Document, stored.Id);
        if (!parsed.TryGetValue(out var definition))
        {
            return parsed.Error!;
        }

        var updated = await catalog.UpdateAsync(definition, context.CancellationToken).ConfigureAwait(false);
        return updated.TryGetValue(out var draft) ? WorkflowDefinitionDetailResponse.From(draft, business) : updated.Error!;
    }
}

/// <summary>
/// 发布流程定义。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-definitions/{id}:publish</c>。单条（照 4.x，设计稿 D10）：关联了审批业务（名字等于业务码）的定义先过业务校验
/// （每个结束节点的结论在业务允许的范围里、没有终止节点），再由框架做结构校验，不合法即 <c>WORKFLOW_VALIDATION_001</c>，问题列表在 <c>details.errors</c>。
/// </remarks>
/// <param name="catalog">定义目录。</param>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsUpdate)]
public sealed class PublishWorkflowDefinition(IWorkflowDefinitionCatalog catalog, WorkflowBusinessRegistry business) : IOperation<PublishWorkflowDefinitionRequest, WorkflowDefinitionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.publish";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowDefinitionDetailResponse>> HandleAsync(PublishWorkflowDefinitionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await WorkflowDefinitions.FindAsync(catalog, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var definition))
        {
            return found.Error!;
        }

        if (business.Find(definition.Name.Value) is { } handler)
        {
            var guarded = WorkflowDefinitionGuard.Check(definition, handler.Descriptor);
            if (guarded.IsFailure)
            {
                return guarded.Error!;
            }
        }

        var published = await catalog.PublishAsync(definition.Id, context.CancellationToken).ConfigureAwait(false);
        return published.TryGetValue(out var result) ? WorkflowDefinitionDetailResponse.From(result, business) : published.Error!;
    }
}

/// <summary>
/// 按名字开新版本。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-definitions:new-version</c>。以这个名字的最新版本为底复制出新草稿（照 4.x 归新建权限）。
/// </remarks>
/// <param name="catalog">定义目录。</param>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsCreate)]
public sealed class NewWorkflowDefinitionVersion(IWorkflowDefinitionCatalog catalog, WorkflowBusinessRegistry business) : IOperation<NewWorkflowDefinitionVersionRequest, WorkflowDefinitionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.new-version";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowDefinitionDetailResponse>> HandleAsync(NewWorkflowDefinitionVersionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await catalog.NewVersionAsync(new WorkflowName(request.Name), context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var draft) ? WorkflowDefinitionDetailResponse.From(draft, business) : created.Error!;
    }
}

/// <summary>
/// 停用流程定义。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-definitions:disable</c>。批量，只停已发布的；存量实例照常跑完。
/// </remarks>
/// <param name="catalog">定义目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsUpdate)]
public sealed class DisableWorkflowDefinitions(IWorkflowDefinitionCatalog catalog) : IOperation<WorkflowDefinitionIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.disable";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowDefinitionIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        return WorkflowDefinitions.Batch(ids, await catalog.DisableAsync(ids.Select(WorkflowIds.ToEngine).ToList(), context.CancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// 归档流程定义。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-definitions:archive</c>。批量，任何状态都能归档。
/// </remarks>
/// <param name="catalog">定义目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsUpdate)]
public sealed class ArchiveWorkflowDefinitions(IWorkflowDefinitionCatalog catalog) : IOperation<WorkflowDefinitionIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.archive";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowDefinitionIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        return WorkflowDefinitions.Batch(ids, await catalog.ArchiveAsync(ids.Select(WorkflowIds.ToEngine).ToList(), context.CancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// 删除流程定义草稿。
/// </summary>
/// <remarks>
/// <c>DELETE /api/workflow-definitions?ids=</c>。批量，只删草稿（别的状态是框架的 <c>WORKFLOW_CONFLICT_002</c>）。
/// </remarks>
/// <param name="catalog">定义目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.DefinitionsDelete)]
public sealed class DeleteWorkflowDefinitions(IWorkflowDefinitionCatalog catalog) : IOperation<WorkflowDefinitionIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowDefinitionIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        var outcomes = await catalog.DeleteAsync(ids.Select(WorkflowIds.ToEngine).ToList(), context.CancellationToken).ConfigureAwait(false);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        for (var i = 0; i < ids.Count; i++)
        {
            if (outcomes[i].IsSuccess)
            {
                succeeded.Add(ids[i]);
            }
            else
            {
                failed.Add(new BatchFailure(ids[i], outcomes[i].Error!.Code));
            }
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 审批业务目录。
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-definitions:business-types</c>。各包登记的审批业务：业务码（即流程定义名）、显示名文案键、单据组件、发起变量与允许的结论，设计器「关联业务」用。
/// </remarks>
/// <param name="business">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Permission = WorkflowPermissions.DefinitionsRead)]
public sealed class ListWorkflowBusinessTypes(WorkflowBusinessRegistry business) : IOperation<ListWorkflowBusinessTypesRequest, WorkflowBusinessTypesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.business-types";

    /// <inheritdoc />
    public ValueTask<Result<WorkflowBusinessTypesResponse>> HandleAsync(ListWorkflowBusinessTypesRequest request, InvocationContext context)
        => ValueTask.FromResult(Result.Success(new WorkflowBusinessTypesResponse(business.Descriptors.Select(WorkflowBusinessTypeResponse.From).ToList())));
}

/// <summary>
/// 活动类型目录。
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-definitions:activity-types</c>。框架登记的活动里设计器能放的那些（照 4.x 的节点，不含 Script 与框架 5 新增的事件网关、补偿、外部任务，W4、W5）。
/// </remarks>
/// <param name="catalog">定义目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Permission = WorkflowPermissions.DefinitionsRead)]
public sealed class ListWorkflowActivityTypes(IWorkflowDefinitionCatalog catalog) : IOperation<ListWorkflowActivityTypesRequest, WorkflowActivityTypesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-definitions.activity-types";

    /// <inheritdoc />
    public ValueTask<Result<WorkflowActivityTypesResponse>> HandleAsync(ListWorkflowActivityTypesRequest request, InvocationContext context)
        => ValueTask.FromResult(Result.Success(new WorkflowActivityTypesResponse(catalog.ActivityTypes().Where(static a => WorkflowDefinitionLimits.ActivityTypes.Contains(a.Type)).Select(WorkflowActivityTypeResponse.From).ToList())));
}

/// <summary>
/// 定义操作共用的读取与换算。
/// </summary>
internal static class WorkflowDefinitions
{
    public static async Task<Result<WorkflowDefinition>> FindAsync(IWorkflowDefinitionCatalog catalog, Guid id, CancellationToken cancellationToken)
    {
        var found = await catalog.FindAsync(WorkflowIds.ToEngine(id), cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var maybe))
        {
            return found.Error!;
        }

        return maybe.TryGetValue(out var definition)
            ? definition
            : Error.NotFound(WorkflowErrors.DefinitionNotFound, WorkflowMessages.DefinitionNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 读请求里的定义文档，ID 换成给定的（框架要求文档带 ID 与版本）。
    /// </summary>
    public static Result<WorkflowDefinition> Parse(string document, string id)
    {
        JsonObject? json;
        try
        {
            json = JsonNode.Parse(document) as JsonObject;
        }
        catch (JsonException)
        {
            json = null;
        }

        if (json is null)
        {
            // 不是 JSON 对象：交给框架报 WORKFLOW_VALIDATION_001
            return WorkflowJson.DeserializeDefinition(document);
        }

        json["id"] = id;

        // 版本由目录决定（建草稿按同名最大版本加一，改草稿沿用已存的）；文档没写时补上合法的占位值
        json["version"] ??= 1;
        return WorkflowJson.DeserializeDefinition(json.ToJsonString());
    }

    public static BatchResult Batch(IReadOnlyList<Guid> ids, IReadOnlyList<Result<WorkflowDefinition>> outcomes)
    {
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        for (var i = 0; i < ids.Count; i++)
        {
            if (outcomes[i].IsSuccess)
            {
                succeeded.Add(ids[i]);
            }
            else
            {
                failed.Add(new BatchFailure(ids[i], outcomes[i].Error!.Code));
            }
        }

        return new BatchResult(succeeded, failed);
    }
}
