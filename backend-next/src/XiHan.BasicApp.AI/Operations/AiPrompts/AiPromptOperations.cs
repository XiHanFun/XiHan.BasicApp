// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.AiPrompts;

/// <summary>
/// 提示词列表
/// </summary>
/// <remarks>
/// <c>POST /api/ai-prompts:list</c>。不带正文；筛选与排序见 <see cref="AiPromptQueries"/>
/// </remarks>
/// <param name="queries">提示词查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiPromptsRead)]
public sealed class ListAiPrompts(AiPromptQueries queries) : IOperation<PageRequest, PageResult<AiPromptResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-prompts.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<AiPromptResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 提示词详情
/// </summary>
/// <remarks>
/// <c>GET /api/ai-prompts/{id}</c>。带正文
/// </remarks>
/// <param name="service">提示词规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiPromptsRead)]
public sealed class GetAiPrompt(AiPromptService service) : IOperation<AiPromptIdRequest, AiPromptDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-prompts.get";

    /// <inheritdoc />
    public async ValueTask<Result<AiPromptDetailResponse>> HandleAsync(AiPromptIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var prompt) ? AiPromptDetailResponse.From(prompt) : found.Error!;
    }
}

/// <summary>
/// 新建提示词
/// </summary>
/// <remarks>
/// <c>POST /api/ai-prompts</c>。编码已被占用 <c>AI_CONFLICT_103</c>（并发重复由部分唯一索引兜住）
/// </remarks>
/// <param name="prompts">提示词仓储</param>
/// <param name="service">提示词规则</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiPromptsCreate)]
public sealed class CreateAiPrompt(IRepository<SysAiPrompt, Guid> prompts, AiPromptService service, IIdGenerator<Guid> ids) : IOperation<CreateAiPromptRequest, AiPromptDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-prompts.create";

    /// <inheritdoc />
    public async ValueTask<Result<AiPromptDetailResponse>> HandleAsync(CreateAiPromptRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var code = request.Code.Trim();
        if (await service.CodeTakenAsync(code, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(AIErrors.PromptCodeTaken, AIMessages.PromptCodeTaken, MessageArguments.Of(("code", code)));
        }

        var prompt = new SysAiPrompt
        {
            Id = ids.NewId(),
            Code = code,
            Name = request.Name.Trim(),
            Category = AiProviderService.Trimmed(request.Category),
            Version = AiProviderService.Trimmed(request.Version),
            Content = request.Content,
            Status = request.Status,
            Order = request.Order,
            Remark = AiProviderService.Trimmed(request.Remark),
        };
        var added = await prompts.AddAsync(prompt, context.CancellationToken).ConfigureAwait(false);
        return added.IsSuccess ? AiPromptDetailResponse.From(prompt) : added.Error!;
    }
}

/// <summary>
/// 修改提示词
/// </summary>
/// <remarks>
/// <c>PUT /api/ai-prompts/{id}</c>。编码不可改；交回的并发戳与当前的不同即冲突
/// </remarks>
/// <param name="prompts">提示词仓储</param>
/// <param name="service">提示词规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiPromptsUpdate)]
public sealed class UpdateAiPrompt(IRepository<SysAiPrompt, Guid> prompts, AiPromptService service) : IOperation<UpdateAiPromptRequest, AiPromptDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-prompts.update";

    /// <inheritdoc />
    public async ValueTask<Result<AiPromptDetailResponse>> HandleAsync(UpdateAiPromptRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var prompt))
        {
            return found.Error!;
        }

        // 带上读到的并发戳：与当前行不同即由仓储报冲突
        prompt.ConcurrencyStamp = request.ConcurrencyStamp;
        prompt.Name = request.Name.Trim();
        prompt.Category = AiProviderService.Trimmed(request.Category);
        prompt.Version = AiProviderService.Trimmed(request.Version);
        prompt.Content = request.Content;
        prompt.Order = request.Order;
        prompt.Remark = AiProviderService.Trimmed(request.Remark);
        var updated = await prompts.UpdateAsync(prompt, context.CancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? AiPromptDetailResponse.From(prompt) : updated.Error!;
    }
}

/// <summary>
/// 启用或停用提示词
/// </summary>
/// <remarks>
/// <c>POST /api/ai-prompts/{id}:set-status</c>。交回的并发戳与当前的不同即冲突；给了备注即替换备注（照 4.x）
/// </remarks>
/// <param name="prompts">提示词仓储</param>
/// <param name="service">提示词规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiPromptsUpdate)]
public sealed class SetAiPromptStatus(IRepository<SysAiPrompt, Guid> prompts, AiPromptService service) : IOperation<SetAiPromptStatusRequest, AiPromptDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-prompts.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<AiPromptDetailResponse>> HandleAsync(SetAiPromptStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var prompt))
        {
            return found.Error!;
        }

        prompt.ConcurrencyStamp = request.ConcurrencyStamp;
        prompt.Status = request.Status;
        prompt.Remark = AiProviderService.Trimmed(request.Remark) ?? prompt.Remark;
        var updated = await prompts.UpdateAsync(prompt, context.CancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? AiPromptDetailResponse.From(prompt) : updated.Error!;
    }
}

/// <summary>
/// 批量删除提示词
/// </summary>
/// <remarks>
/// <c>DELETE /api/ai-prompts?ids=...</c>。逐项：不存在 <c>AI_NOTFOUND_102</c>。软删除，同编码可以重建
/// </remarks>
/// <param name="prompts">提示词仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiPromptsDelete)]
public sealed class DeleteAiPrompts(IRepository<SysAiPrompt, Guid> prompts) : IOperation<AiPromptIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-prompts.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(AiPromptIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await prompts.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static p => p.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var prompt))
            {
                failed.Add(new BatchFailure(id, AIErrors.PromptNotFound));
                continue;
            }

            var removed = await prompts.RemoveAsync(prompt, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
