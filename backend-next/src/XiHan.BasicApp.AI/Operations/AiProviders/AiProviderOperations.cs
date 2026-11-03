// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.AI;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.AI.Abstractions.Providers;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.AiProviders;

/// <summary>
/// 提供商列表
/// </summary>
/// <remarks>
/// <c>POST /api/ai-providers:list</c>。筛选与排序见 <see cref="AiProviderQueries"/>
/// </remarks>
/// <param name="queries">提供商查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersRead)]
public sealed class ListAiProviders(AiProviderQueries queries) : IOperation<PageRequest, PageResult<AiProviderResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<AiProviderResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 提供商详情
/// </summary>
/// <remarks>
/// <c>GET /api/ai-providers/{id}</c>。API 密钥不回显
/// </remarks>
/// <param name="service">提供商规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersRead)]
public sealed class GetAiProvider(AiProviderService service) : IOperation<AiProviderIdRequest, AiProviderResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.get";

    /// <inheritdoc />
    public async ValueTask<Result<AiProviderResponse>> HandleAsync(AiProviderIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var provider) ? AiProviderResponse.From(provider) : found.Error!;
    }
}

/// <summary>
/// 新建提供商
/// </summary>
/// <remarks>
/// <c>POST /api/ai-providers</c>。编码已被占用 <c>AI_CONFLICT_101</c>（并发重复由部分唯一索引兜住）；API 密钥加密存储；设为缺省时别的提供商取消缺省（照 4.x）
/// </remarks>
/// <param name="providers">提供商仓储</param>
/// <param name="service">提供商规则</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersCreate)]
public sealed class CreateAiProvider(IRepository<SysAiProvider, Guid> providers, AiProviderService service, IIdGenerator<Guid> ids) : IOperation<CreateAiProviderRequest, AiProviderResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.create";

    /// <inheritdoc />
    public async ValueTask<Result<AiProviderResponse>> HandleAsync(CreateAiProviderRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var code = request.Code.Trim();
        if (await service.CodeTakenAsync(code, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(AIErrors.ProviderCodeTaken, AIMessages.ProviderCodeTaken, MessageArguments.Of(("code", code)));
        }

        var provider = new SysAiProvider
        {
            Id = ids.NewId(),
            Code = code,
            Name = request.Name.Trim(),
            Vendor = request.Vendor.Trim(),
            ChatModel = request.ChatModel.Trim(),
            EmbeddingModel = AiProviderService.Trimmed(request.EmbeddingModel),
            Endpoint = AiProviderService.Trimmed(request.Endpoint),
            ApiKey = service.Protect(request.ApiKey),
            MaxOutputTokens = request.MaxOutputTokens,
            Temperature = request.Temperature,
            TimeoutSeconds = request.TimeoutSeconds,
            IsDefault = request.IsDefault,
            Status = request.Status,
            Order = request.Order,
            Remark = AiProviderService.Trimmed(request.Remark),
        };
        if (provider.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(provider.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        var added = await providers.AddAsync(provider, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        service.Changed();
        return AiProviderResponse.From(provider);
    }
}

/// <summary>
/// 修改提供商
/// </summary>
/// <remarks>
/// <c>PUT /api/ai-providers/{id}</c>。编码不可改；API 密钥留空即保留（照 4.x）；交回的并发戳与当前的不同即冲突
/// 设为缺省时别的提供商取消缺省（照 4.x）。提交之后重载 Provider 目录，新的调用即用新配置
/// </remarks>
/// <param name="providers">提供商仓储</param>
/// <param name="service">提供商规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersUpdate)]
public sealed class UpdateAiProvider(IRepository<SysAiProvider, Guid> providers, AiProviderService service) : IOperation<UpdateAiProviderRequest, AiProviderResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.update";

    /// <inheritdoc />
    public async ValueTask<Result<AiProviderResponse>> HandleAsync(UpdateAiProviderRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var provider))
        {
            return found.Error!;
        }

        // 带上读到的并发戳：与当前行不同即由仓储报冲突
        provider.ConcurrencyStamp = request.ConcurrencyStamp;
        provider.Name = request.Name.Trim();
        provider.Vendor = request.Vendor.Trim();
        provider.ChatModel = request.ChatModel.Trim();
        provider.EmbeddingModel = AiProviderService.Trimmed(request.EmbeddingModel);
        provider.Endpoint = AiProviderService.Trimmed(request.Endpoint);
        provider.ApiKey = service.Protect(request.ApiKey) ?? provider.ApiKey;
        provider.MaxOutputTokens = request.MaxOutputTokens;
        provider.Temperature = request.Temperature;
        provider.TimeoutSeconds = request.TimeoutSeconds;
        provider.Order = request.Order;
        provider.Remark = AiProviderService.Trimmed(request.Remark);
        if (request.IsDefault && !provider.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(provider.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        provider.IsDefault = request.IsDefault;
        var updated = await providers.UpdateAsync(provider, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        service.Changed();
        return AiProviderResponse.From(provider);
    }
}

/// <summary>
/// 启用或停用提供商
/// </summary>
/// <remarks>
/// <c>POST /api/ai-providers/{id}:set-status</c>。交回的并发戳与当前的不同即冲突；给了备注即替换备注（照 4.x）
/// 停用的提供商不进 Provider 目录，用到它的调用即 <c>AI_NOTFOUND_001</c>
/// </remarks>
/// <param name="providers">提供商仓储</param>
/// <param name="service">提供商规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersUpdate)]
public sealed class SetAiProviderStatus(IRepository<SysAiProvider, Guid> providers, AiProviderService service) : IOperation<SetAiProviderStatusRequest, AiProviderResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<AiProviderResponse>> HandleAsync(SetAiProviderStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var provider))
        {
            return found.Error!;
        }

        provider.ConcurrencyStamp = request.ConcurrencyStamp;
        provider.Status = request.Status;
        provider.Remark = AiProviderService.Trimmed(request.Remark) ?? provider.Remark;
        var updated = await providers.UpdateAsync(provider, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        service.Changed();
        return AiProviderResponse.From(provider);
    }
}

/// <summary>
/// 设为缺省提供商
/// </summary>
/// <remarks>
/// <c>POST /api/ai-providers/{id}:set-default</c>。停用的不能设 <c>AI_CONFLICT_102</c>；别的提供商取消缺省（照 4.x）
/// </remarks>
/// <param name="providers">提供商仓储</param>
/// <param name="service">提供商规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersUpdate)]
public sealed class SetDefaultAiProvider(IRepository<SysAiProvider, Guid> providers, AiProviderService service) : IOperation<AiProviderIdRequest, AiProviderResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.set-default";

    /// <inheritdoc />
    public async ValueTask<Result<AiProviderResponse>> HandleAsync(AiProviderIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var provider))
        {
            return found.Error!;
        }

        if (provider.Status != AiStatus.Active)
        {
            return Error.Conflict(AIErrors.ProviderDisabled, AIMessages.ProviderDisabled, MessageArguments.Of(("code", provider.Code)));
        }

        var cleared = await service.ClearOtherDefaultsAsync(provider.Id, context.CancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return cleared.Error!;
        }

        if (!provider.IsDefault)
        {
            provider.IsDefault = true;
            var updated = await providers.UpdateAsync(provider, context.CancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        service.Changed();
        return AiProviderResponse.From(provider);
    }
}

/// <summary>
/// 批量删除提供商
/// </summary>
/// <remarks>
/// <c>DELETE /api/ai-providers?ids=...</c>。逐项：不存在 <c>AI_NOTFOUND_101</c>。软删除，同编码可以重建；删除后不再进 Provider 目录
/// </remarks>
/// <param name="providers">提供商仓储</param>
/// <param name="service">提供商规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersDelete)]
public sealed class DeleteAiProviders(IRepository<SysAiProvider, Guid> providers, AiProviderService service) : IOperation<AiProviderIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(AiProviderIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await providers.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static p => p.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var provider))
            {
                failed.Add(new BatchFailure(id, AIErrors.ProviderNotFound));
                continue;
            }

            var removed = await providers.RemoveAsync(provider, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        if (succeeded.Count > 0)
        {
            service.Changed();
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 测试提供商的连接
/// </summary>
/// <remarks>
/// <c>POST /api/ai-providers/{id}:test</c>。照 4.x：按当前配置临时建一次客户端（停用的也能测，用完即释放），聊天发一句 <c>ping</c>（最大输出 16 个令牌），
/// 配了嵌入模型再嵌入一次并回报维度；不通不是失败的调用，结果里给提供商的原因。超时取提供商的设置，不落库
/// API 密钥解不开即 <c>AI_UNAVAILABLE_101</c>
/// </remarks>
/// <param name="service">提供商规则</param>
/// <param name="chatFactories">聊天客户端工厂</param>
/// <param name="embeddingFactories">嵌入生成器工厂</param>
/// <param name="clock">时钟</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.AiProvidersExecute)]
public sealed class TestAiProvider(AiProviderService service, IEnumerable<IChatClientFactory> chatFactories, IEnumerable<IEmbeddingGeneratorFactory> embeddingFactories, TimeProvider clock) : IOperation<AiProviderIdRequest, AiProviderTestResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "ai-providers.test";

    /// <summary>
    /// 聊天探测的最大输出令牌数
    /// </summary>
    /// <remarks>
    /// 照 4.x
    /// </remarks>
    private const int ProbeOutputTokens = 16;

    /// <summary>
    /// 探测发的文本
    /// </summary>
    /// <remarks>
    /// 照 4.x
    /// </remarks>
    private const string ProbeText = "ping";

    /// <inheritdoc />
    public async ValueTask<Result<AiProviderTestResponse>> HandleAsync(AiProviderIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var provider))
        {
            return found.Error!;
        }

        var built = service.OptionsOf(provider);
        if (!built.TryGetValue(out var options))
        {
            return built.Error!;
        }

        var chatFactory = chatFactories.LastOrDefault(f => string.Equals(f.Kind, options.Kind, StringComparison.OrdinalIgnoreCase));
        var embeddingFactory = embeddingFactories.LastOrDefault(f => string.Equals(f.Kind, options.Kind, StringComparison.OrdinalIgnoreCase));
        if (chatFactory is null || embeddingFactory is null)
        {
            return Error.Unavailable(AIErrors.ProviderKindUnavailable, AIMessages.ProviderKindUnavailable);
        }

        var chat = await ProbeChatAsync(chatFactory, options, context).ConfigureAwait(false);
        var embedding = string.IsNullOrWhiteSpace(options.EmbeddingModel) ? null : await ProbeEmbeddingAsync(embeddingFactory, options, context).ConfigureAwait(false);
        return new AiProviderTestResponse(chat.Success && (embedding?.Success ?? true), chat, embedding);
    }

    /// <summary>
    /// 聊天模型发一句 <c>ping</c>
    /// </summary>
    private async Task<AiProviderProbe> ProbeChatAsync(IChatClientFactory factory, AIProviderOptions options, InvocationContext context)
    {
        var model = options.ChatModel ?? string.Empty;
        var started = clock.GetTimestamp();
        var created = factory.Create(options);
        if (!created.TryGetValue(out var client))
        {
            return new AiProviderProbe(false, context.Localize(created.Error!), Elapsed(started), model, null);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, ProbeText)], new ChatOptions { MaxOutputTokens = ProbeOutputTokens }, timeout.Token).ConfigureAwait(false);
            return new AiProviderProbe(true, response.FinishReason?.ToString(), Elapsed(started), model, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            return new AiProviderProbe(false, exception.Message, Elapsed(started), model, null);
        }
        finally
        {
            (client as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// 嵌入模型嵌入一次 <c>ping</c>
    /// </summary>
    private async Task<AiProviderProbe> ProbeEmbeddingAsync(IEmbeddingGeneratorFactory factory, AIProviderOptions options, InvocationContext context)
    {
        var model = options.EmbeddingModel ?? string.Empty;
        var started = clock.GetTimestamp();
        var created = factory.Create(options);
        if (!created.TryGetValue(out var generator))
        {
            return new AiProviderProbe(false, context.Localize(created.Error!), Elapsed(started), model, null);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            var vector = await generator.GenerateVectorAsync(ProbeText, cancellationToken: timeout.Token).ConfigureAwait(false);
            return new AiProviderProbe(true, null, Elapsed(started), model, vector.Length);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            return new AiProviderProbe(false, exception.Message, Elapsed(started), model, null);
        }
        finally
        {
            generator.Dispose();
        }
    }

    private long Elapsed(long started) => (long)clock.GetElapsedTime(started).TotalMilliseconds;
}
