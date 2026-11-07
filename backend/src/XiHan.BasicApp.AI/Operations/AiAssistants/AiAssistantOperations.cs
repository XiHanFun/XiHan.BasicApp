// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.AI.Abstractions.Knowledge;
using XiHan.Framework.AI.Abstractions.Tools;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.AiAssistants;

/// <summary>
/// 助手列表
/// </summary>
/// <remarks>
/// <c>POST /api/ai-assistants:list</c>。筛选与排序见 <see cref="AiAssistantQueries"/>
/// </remarks>
/// <param name="queries">助手查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsRead)]
public sealed class ListAiAssistants(AiAssistantQueries queries) : IOperation<PageRequest, PageResult<AiAssistantResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<AiAssistantResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 助手详情
/// </summary>
/// <remarks>
/// <c>GET /api/ai-assistants/{id}</c>
/// </remarks>
/// <param name="service">助手规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsRead)]
public sealed class GetAiAssistant(AiAssistantService service) : IOperation<AiAssistantIdRequest, AiAssistantResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.get";

    /// <inheritdoc />
    public async ValueTask<Result<AiAssistantResponse>> HandleAsync(AiAssistantIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var assistant) ? AiAssistantResponse.From(assistant) : found.Error!;
    }
}

/// <summary>
/// 新建助手
/// </summary>
/// <remarks>
/// <c>POST /api/ai-assistants</c>。编码已被占用 <c>AI_CONFLICT_105</c>（并发重复由部分唯一索引兜住）；引用不存在或已停用 <c>AI_VALIDATION_104</c> ～ <c>106</c>；
/// 停用的不能设为缺省 <c>AI_CONFLICT_106</c>；设为缺省时别的助手取消缺省
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="service">助手规则</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsCreate)]
public sealed class CreateAiAssistant(IRepository<SysAiAssistant, Guid> assistants, AiAssistantService service, IIdGenerator<Guid> ids) : IOperation<CreateAiAssistantRequest, AiAssistantResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.create";

    /// <inheritdoc />
    public async ValueTask<Result<AiAssistantResponse>> HandleAsync(CreateAiAssistantRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var code = request.Code.Trim();
        if (await service.CodeTakenAsync(code, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(AIErrors.AssistantCodeTaken, AIMessages.AssistantCodeTaken, MessageArguments.Of(("code", code)));
        }

        if (request.IsDefault && request.Status != AiStatus.Active)
        {
            return Error.Conflict(AIErrors.AssistantDisabled, AIMessages.AssistantDisabled, MessageArguments.Of(("code", code)));
        }

        var promptCode = AiProviderService.Trimmed(request.PromptCode);
        var providerCode = AiProviderService.Trimmed(request.ProviderCode);
        var knowledgeProviderCode = AiProviderService.Trimmed(request.KnowledgeProviderCode);
        var tools = request.Tools?.Distinct(StringComparer.Ordinal).ToList() ?? [];
        var referenced = await service.CheckReferencesAsync(promptCode, providerCode, knowledgeProviderCode, tools, context.CancellationToken).ConfigureAwait(false);
        if (referenced.IsFailure)
        {
            return referenced.Error!;
        }

        var assistant = new SysAiAssistant
        {
            Id = ids.NewId(),
            Code = code,
            Name = request.Name.Trim(),
            Avatar = AiProviderService.Trimmed(request.Avatar),
            Description = AiProviderService.Trimmed(request.Description),
            Greeting = AiProviderService.Trimmed(request.Greeting),
            PromptCode = promptCode,
            ProviderCode = providerCode,
            EnableKnowledge = request.EnableKnowledge,
            KnowledgeProviderCode = knowledgeProviderCode,
            KnowledgeTopK = request.KnowledgeTopK,
            HistoryRounds = request.HistoryRounds,
            Tools = AiAssistantService.SerializeTools(tools),
            IsDefault = request.IsDefault,
            Status = request.Status,
            Order = request.Order,
            Remark = AiProviderService.Trimmed(request.Remark),
        };
        var added = await assistants.AddAsync(assistant, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        if (assistant.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(assistant.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        return AiAssistantResponse.From(assistant);
    }
}

/// <summary>
/// 修改助手
/// </summary>
/// <remarks>
/// <c>PUT /api/ai-assistants/{id}</c>。编码不可改；交回的并发戳与当前的不同即冲突；引用的核对同新建；设为缺省时别的助手取消缺省
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="service">助手规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsUpdate)]
public sealed class UpdateAiAssistant(IRepository<SysAiAssistant, Guid> assistants, AiAssistantService service) : IOperation<UpdateAiAssistantRequest, AiAssistantResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.update";

    /// <inheritdoc />
    public async ValueTask<Result<AiAssistantResponse>> HandleAsync(UpdateAiAssistantRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var assistant))
        {
            return found.Error!;
        }

        if (request.IsDefault && assistant.Status != AiStatus.Active)
        {
            return Error.Conflict(AIErrors.AssistantDisabled, AIMessages.AssistantDisabled, MessageArguments.Of(("code", assistant.Code)));
        }

        var promptCode = AiProviderService.Trimmed(request.PromptCode);
        var providerCode = AiProviderService.Trimmed(request.ProviderCode);
        var knowledgeProviderCode = AiProviderService.Trimmed(request.KnowledgeProviderCode);
        var tools = request.Tools?.Distinct(StringComparer.Ordinal).ToList() ?? [];
        var referenced = await service.CheckReferencesAsync(promptCode, providerCode, knowledgeProviderCode, tools, context.CancellationToken).ConfigureAwait(false);
        if (referenced.IsFailure)
        {
            return referenced.Error!;
        }

        // 带上读到的并发戳：与当前行不同即由仓储报冲突
        assistant.ConcurrencyStamp = request.ConcurrencyStamp;
        assistant.Name = request.Name.Trim();
        assistant.Avatar = AiProviderService.Trimmed(request.Avatar);
        assistant.Description = AiProviderService.Trimmed(request.Description);
        assistant.Greeting = AiProviderService.Trimmed(request.Greeting);
        assistant.PromptCode = promptCode;
        assistant.ProviderCode = providerCode;
        assistant.EnableKnowledge = request.EnableKnowledge;
        assistant.KnowledgeProviderCode = knowledgeProviderCode;
        assistant.KnowledgeTopK = request.KnowledgeTopK;
        assistant.HistoryRounds = request.HistoryRounds;
        assistant.Tools = AiAssistantService.SerializeTools(tools);
        assistant.IsDefault = request.IsDefault;
        assistant.Order = request.Order;
        assistant.Remark = AiProviderService.Trimmed(request.Remark);
        var updated = await assistants.UpdateAsync(assistant, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        if (assistant.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(assistant.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        return AiAssistantResponse.From(assistant);
    }
}

/// <summary>
/// 启用或停用助手
/// </summary>
/// <remarks>
/// <c>POST /api/ai-assistants/{id}:set-status</c>。交回的并发戳与当前的不同即冲突；给了备注即替换备注
/// 停用后不出现在聊天的可用助手里，已有会话不能再继续提问
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="service">助手规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsUpdate)]
public sealed class SetAiAssistantStatus(IRepository<SysAiAssistant, Guid> assistants, AiAssistantService service) : IOperation<SetAiAssistantStatusRequest, AiAssistantResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<AiAssistantResponse>> HandleAsync(SetAiAssistantStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var assistant))
        {
            return found.Error!;
        }

        assistant.ConcurrencyStamp = request.ConcurrencyStamp;
        assistant.Status = request.Status;
        assistant.Remark = AiProviderService.Trimmed(request.Remark) ?? assistant.Remark;
        var updated = await assistants.UpdateAsync(assistant, context.CancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? AiAssistantResponse.From(assistant) : updated.Error!;
    }
}

/// <summary>
/// 设为缺省助手
/// </summary>
/// <remarks>
/// <c>POST /api/ai-assistants/{id}:set-default</c>。停用的不能设 <c>AI_CONFLICT_106</c>；别的助手取消缺省
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="service">助手规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsUpdate)]
public sealed class SetDefaultAiAssistant(IRepository<SysAiAssistant, Guid> assistants, AiAssistantService service) : IOperation<AiAssistantIdRequest, AiAssistantResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.set-default";

    /// <inheritdoc />
    public async ValueTask<Result<AiAssistantResponse>> HandleAsync(AiAssistantIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var assistant))
        {
            return found.Error!;
        }

        if (assistant.Status != AiStatus.Active)
        {
            return Error.Conflict(AIErrors.AssistantDisabled, AIMessages.AssistantDisabled, MessageArguments.Of(("code", assistant.Code)));
        }

        var cleared = await service.ClearOtherDefaultsAsync(assistant.Id, context.CancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return cleared.Error!;
        }

        if (!assistant.IsDefault)
        {
            assistant.IsDefault = true;
            var updated = await assistants.UpdateAsync(assistant, context.CancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        return AiAssistantResponse.From(assistant);
    }
}

/// <summary>
/// 批量删除助手
/// </summary>
/// <remarks>
/// <c>DELETE /api/ai-assistants?ids=...</c>。逐项：不存在 <c>AI_NOTFOUND_104</c>。软删除，同编码可以重建；已有会话不删，但不能再继续提问
/// </remarks>
/// <param name="assistants">助手仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsDelete)]
public sealed class DeleteAiAssistants(IRepository<SysAiAssistant, Guid> assistants) : IOperation<AiAssistantIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(AiAssistantIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await assistants.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static a => a.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var assistant))
            {
                failed.Add(new BatchFailure(id, AIErrors.AssistantNotFound, AIMessages.AssistantNotFound.L(("id", id))));
                continue;
            }

            var removed = await assistants.RemoveAsync(assistant, context.CancellationToken).ConfigureAwait(false);
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
/// 可勾选的工具
/// </summary>
/// <remarks>
/// <c>GET /api/ai-assistants:tools</c>。从框架的工具目录列出，说明取调用方的区域；目录里的操作工具都是 <c>Expose.Ai</c> 的只读操作（方案 A3，<c>OperationExposureTests</c> 守护）
/// 同时告诉表单有没有向量库：没有时开着「检索知识库」的助手回复会失败（方案 D7b）
/// </remarks>
/// <param name="tools">工具目录</param>
/// <param name="services">本作用域的服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AIPermissions.AiAssistantsRead)]
public sealed class ListAiAssistantTools(IAIToolCatalog tools, IServiceProvider services) : IOperation<AiAssistantToolsRequest, AiAssistantToolsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-assistants.tools";

    /// <inheritdoc />
    public ValueTask<Result<AiAssistantToolsResponse>> HandleAsync(AiAssistantToolsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var items = tools.Bind(context).Select(static tool => new AiAssistantToolItem(tool.Name, tool.Description)).OrderBy(static item => item.Name, StringComparer.Ordinal).ToList();
        return ValueTask.FromResult<Result<AiAssistantToolsResponse>>(new AiAssistantToolsResponse(items, services.GetService<IKnowledgeStore>() is not null));
    }
}
