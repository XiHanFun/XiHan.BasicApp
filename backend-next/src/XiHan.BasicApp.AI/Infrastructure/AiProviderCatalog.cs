// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.AI.Abstractions.Providers;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using FrameworkAIErrors = XiHan.Framework.AI.Abstractions.AIErrors;
using FrameworkAIMessages = XiHan.Framework.AI.Abstractions.AIMessages;

namespace XiHan.BasicApp.AI.Infrastructure;

/// <summary>
/// 落库的 Provider 目录
/// </summary>
/// <remarks>
/// 取代框架按配置建的目录（方案 A1，照 4.x 落库）：启用中的提供商按编码映射成框架的 Provider 配置，聊天、嵌入与知识库都经它解析客户端
/// 框架的目录接口是同步的，所以持一份内存快照：宿主启动时装载，提供商写入的调用提交之后重载（<c>AiProviderCatalogMiddleware</c>）；
/// 配了 Redis 时写入方另写一个戳键，别的实例经共享缓存的变更通知重载，没配是单实例。重载只丢掉配置变了或没了的提供商缓存的客户端
/// 解不开 API 密钥的提供商记 Warning 后不进快照，按未配置处理（照 4.x 的 fail-closed，不当明文用）
/// </remarks>
public sealed partial class AiProviderCatalog : IAIProviderCatalog, IDisposable
{
    /// <summary>
    /// 跨实例重载用的戳键
    /// </summary>
    public static readonly CacheKey StampKey = new("ai:providers:stamp");

    private readonly IServiceScopeFactory _scopes;
    private readonly ISecretProtector _secrets;
    private readonly ILogger<AiProviderCatalog> _logger;
    private readonly Dictionary<string, IChatClientFactory> _chatFactories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IEmbeddingGeneratorFactory> _embeddingFactories = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();
    private readonly Dictionary<string, IChatClient> _chatClients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> _generators = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _reloading = new(1, 1);
    private readonly IDisposable? _changes;
    private Snapshot _snapshot = Snapshot.Empty;

    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="scopes">作用域工厂</param>
    /// <param name="secrets">密钥保护</param>
    /// <param name="cache">共享缓存（有变更通知时订阅戳键）</param>
    /// <param name="chatFactories">聊天客户端工厂</param>
    /// <param name="embeddingFactories">嵌入生成器工厂</param>
    /// <param name="logger">日志</param>
    public AiProviderCatalog(IServiceScopeFactory scopes, ISecretProtector secrets, ISharedCache cache, IEnumerable<IChatClientFactory> chatFactories, IEnumerable<IEmbeddingGeneratorFactory> embeddingFactories, ILogger<AiProviderCatalog> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(chatFactories);
        ArgumentNullException.ThrowIfNull(embeddingFactories);
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        foreach (var factory in chatFactories)
        {
            _chatFactories[factory.Kind.ToLowerInvariant()] = factory;
        }

        foreach (var factory in embeddingFactories)
        {
            _embeddingFactories[factory.Kind.ToLowerInvariant()] = factory;
        }

        _changes = cache.Store is ICacheChangeFeed feed ? feed.Subscribe(OnChanged) : null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Providers => [.. Volatile.Read(ref _snapshot).Options.Keys];

    /// <inheritdoc />
    /// <remarks>
    /// 启用中且标为缺省的那一个；没有为 <see langword="null"/>（照 4.x，不在只有一个时自动取它）
    /// </remarks>
    public string? DefaultProvider => Volatile.Read(ref _snapshot).Default;

    /// <inheritdoc />
    /// <remarks>
    /// 照 4.x 只有一个缺省：与 <see cref="DefaultProvider"/> 是同一个提供商
    /// </remarks>
    public string? DefaultEmbeddingProvider => DefaultProvider;

    /// <inheritdoc />
    public Maybe<AIProviderOptions> Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Volatile.Read(ref _snapshot).Options.TryGetValue(name, out var options) ? Maybe.Some(options) : Maybe.None<AIProviderOptions>();
    }

    /// <inheritdoc />
    public Result<IChatClient> ResolveChatClient(string? name = null)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        var resolved = name ?? snapshot.Default;
        if (resolved is null)
        {
            return Error.NotFound(FrameworkAIErrors.ProviderNotFound, FrameworkAIMessages.DefaultProviderMissing);
        }

        if (!snapshot.Options.TryGetValue(resolved, out var options))
        {
            return NotFound(resolved);
        }

        lock (_lock)
        {
            if (_chatClients.TryGetValue(resolved, out var cached))
            {
                return Result.Success(cached);
            }

            if (!_chatFactories.TryGetValue(options.Kind, out var factory))
            {
                return KindUnknown(options);
            }

            // 创建在锁内：同名并发解析只建一个实例
            var created = factory.Create(options);
            if (created.TryGetValue(out var client))
            {
                _chatClients[resolved] = client;
            }

            return created;
        }
    }

    /// <inheritdoc />
    public Result<IEmbeddingGenerator<string, Embedding<float>>> ResolveEmbeddingGenerator(string? name = null)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        var resolved = name ?? snapshot.Default;
        if (resolved is null)
        {
            return Error.NotFound(FrameworkAIErrors.ProviderNotFound, FrameworkAIMessages.DefaultEmbeddingProviderMissing);
        }

        if (!snapshot.Options.TryGetValue(resolved, out var options))
        {
            return NotFound(resolved);
        }

        lock (_lock)
        {
            if (_generators.TryGetValue(resolved, out var cached))
            {
                return Result.Success(cached);
            }

            if (!_embeddingFactories.TryGetValue(options.Kind, out var factory))
            {
                return KindUnknown(options);
            }

            var created = factory.Create(options);
            if (created.TryGetValue(out var generator))
            {
                _generators[resolved] = generator;
            }

            return created;
        }
    }

    /// <inheritdoc />
    public void Invalidate(string? name = null)
    {
        List<IDisposable> disposables = [];
        lock (_lock)
        {
            if (name is null)
            {
                disposables.AddRange(_chatClients.Values.OfType<IDisposable>());
                disposables.AddRange(_generators.Values.OfType<IDisposable>());
                _chatClients.Clear();
                _generators.Clear();
            }
            else
            {
                if (_chatClients.Remove(name, out var client) && client is IDisposable disposableClient)
                {
                    disposables.Add(disposableClient);
                }

                if (_generators.Remove(name, out var generator) && generator is IDisposable disposableGenerator)
                {
                    disposables.Add(disposableGenerator);
                }
            }
        }

        // 释放放在锁外：释放可能阻塞或抛出，不拖住别的解析
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// 从库里重载快照
    /// </summary>
    /// <remarks>
    /// 只读启用中的提供商；配置变了或没了的提供商丢掉缓存的客户端，下次解析按新配置建
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        await _reloading.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var rows = await StoreScope.ReadAsync(
                _scopes,
                static (services, ct) => services.GetRequiredService<IRepository<SysAiProvider, Guid>>().ListAsync(EntityQuery.All<SysAiProvider>().Where(new Specification<SysAiProvider>(p => p.Status == AiStatus.Active)), ct),
                cancellationToken).ConfigureAwait(false);
            var options = new Dictionary<string, AIProviderOptions>(StringComparer.Ordinal);
            string? fallback = null;
            foreach (var row in rows)
            {
                var built = AiProviderService.OptionsOf(row, _secrets);
                if (!built.TryGetValue(out var item))
                {
                    LogSecretUnreadable(_logger, row.Code);
                    continue;
                }

                options[row.Code] = item;
                if (row.IsDefault)
                {
                    fallback = row.Code;
                }
            }

            var previous = Interlocked.Exchange(ref _snapshot, new Snapshot(options, fallback));
            foreach (var (name, before) in previous.Options)
            {
                if (!options.TryGetValue(name, out var now) || !Same(before, now))
                {
                    Invalidate(name);
                }
            }
        }
        finally
        {
            _reloading.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _changes?.Dispose();
        Invalidate();
        _reloading.Dispose();
    }

    /// <summary>
    /// 共享缓存的变更：戳键变了即别的实例写过提供商
    /// </summary>
    private void OnChanged(CacheChange change)
    {
        if (!change.Keys.Any(static key => string.Equals(key.Value, StampKey.Value, StringComparison.Ordinal))
            && !(change.Prefix is { } prefix && StampKey.Value.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return;
        }

        // 通知在写缓存的线程上同步回调：重载放到线程池，失败只记日志、等下一次写入或重启
        _ = Task.Run(async () =>
        {
            try
            {
                await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogReloadFailed(_logger, exception);
            }
        });
    }

    /// <summary>
    /// 两份配置建出的客户端是否一样
    /// </summary>
    private static bool Same(AIProviderOptions left, AIProviderOptions right)
        => string.Equals(left.Kind, right.Kind, StringComparison.Ordinal)
           && Equals(left.Endpoint, right.Endpoint)
           && string.Equals(left.ApiKey, right.ApiKey, StringComparison.Ordinal)
           && string.Equals(left.ChatModel, right.ChatModel, StringComparison.Ordinal)
           && string.Equals(left.EmbeddingModel, right.EmbeddingModel, StringComparison.Ordinal)
           && left.EmbeddingDimensions == right.EmbeddingDimensions
           && left.Timeout == right.Timeout;

    /// <summary>
    /// 提供商未配置或未启用
    /// </summary>
    private static Error NotFound(string name)
        => Error.NotFound(FrameworkAIErrors.ProviderNotFound, FrameworkAIMessages.ProviderNotFound, MessageArguments.Of(("name", name)));

    /// <summary>
    /// 种类没有工厂
    /// </summary>
    private static Error KindUnknown(AIProviderOptions options)
        => Error.NotFound(FrameworkAIErrors.ProviderNotFound, FrameworkAIMessages.ProviderKindUnknown, MessageArguments.Of(("name", options.Name), ("kind", options.Kind)));

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The API key of AI provider {Code} cannot be decrypted; the provider is treated as not configured.")]
    private static partial void LogSecretUnreadable(ILogger logger, string code);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Reloading the AI providers after a change on another instance failed.")]
    private static partial void LogReloadFailed(ILogger logger, Exception exception);

    /// <summary>
    /// 一份快照：编码 → 配置，与缺省提供商
    /// </summary>
    private sealed record Snapshot(IReadOnlyDictionary<string, AIProviderOptions> Options, string? Default)
    {
        public static Snapshot Empty { get; } = new(new Dictionary<string, AIProviderOptions>(StringComparer.Ordinal), null);
    }
}
