// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.AI.Infrastructure.Configuration;
using XiHan.Framework.AI.Abstractions.Rag;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// DB 配置源测试：覆盖提示词库与 provider 配置源的解析口径（默认 vs 按编码）、
/// 密钥解密、单行解密失败不阻断全表枚举的 fail-closed 降级，以及 RAG 部署级配置的默认值。
/// </summary>
/// <remarks>
/// 两个 store 都是 Singleton + <see cref="IServiceScopeFactory"/> 开作用域解析 Scoped 仓储，
/// 这里用真实的 DI 容器承载仓储替身，既跑通作用域路径又不触碰数据库。
/// </remarks>
public sealed class AiConfigurationStoreTests
{
    /// <summary>
    /// 提示词库命中时必须按"Name 用编码、Description 用名称"的口径映射，映反了上层按名取模板会全部落空。
    /// </summary>
    [Fact]
    public async Task PromptStoreGetAsync_ShouldMapCodeToNameAndNameToDescription()
    {
        var prompt = AiTestHelper.CreatePrompt(7, "greeting");
        prompt.PromptName = "开场白模板";
        prompt.Content = "你是助手。";
        prompt.Version = "v3";
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.GetEnabledByCodeAsync("greeting", "v3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);
        var store = new SaasAiPromptStore(CreateScopeFactory(repository.Object));

        var template = await store.GetAsync("greeting", "v3");

        Assert.NotNull(template);
        Assert.Equal("greeting", template!.Name, StringComparer.Ordinal);
        Assert.Equal("你是助手。", template.Content, StringComparer.Ordinal);
        Assert.Equal("v3", template.Version, StringComparer.Ordinal);
        Assert.Equal("开场白模板", template.Description, StringComparer.Ordinal);
    }

    /// <summary>
    /// 提示词名为空必须直接返回 null，不得为空名去开一次作用域查库。
    /// </summary>
    /// <param name="name">空白模板名。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PromptStoreGetAsync_BlankNameShouldReturnNullWithoutQuerying(string? name)
    {
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        var store = new SaasAiPromptStore(CreateScopeFactory(repository.Object));

        Assert.Null(await store.GetAsync(name!));

        repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 查不到模板必须返回 null（上层按"未配置"处理），不得抛异常。
    /// </summary>
    [Fact]
    public async Task PromptStoreGetAsync_MissingPromptShouldReturnNull()
    {
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.GetEnabledByCodeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiPrompt?)null);
        var store = new SaasAiPromptStore(CreateScopeFactory(repository.Object));

        Assert.Null(await store.GetAsync("missing"));
    }

    /// <summary>
    /// 未指定版本时必须把 null 版本原样传给仓储（由仓储决定取哪一条启用记录）。
    /// </summary>
    [Fact]
    public async Task PromptStoreGetAsync_ShouldForwardNullVersionAndCancellationToken()
    {
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.GetEnabledByCodeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiTestHelper.CreatePrompt(7));
        var store = new SaasAiPromptStore(CreateScopeFactory(repository.Object));
        using var source = new CancellationTokenSource();

        _ = await store.GetAsync("prompt-code", cancellationToken: source.Token);

        repository.Verify(item => item.GetEnabledByCodeAsync("prompt-code", null, source.Token), Times.Once);
    }

    /// <summary>
    /// 列表必须逐条映射且保持仓储给出的顺序。
    /// </summary>
    [Fact]
    public async Task PromptStoreListAsync_ShouldMapEveryPromptInOrder()
    {
        var first = AiTestHelper.CreatePrompt(1, "alpha");
        var second = AiTestHelper.CreatePrompt(2, "beta");
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.GetEnabledListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);
        var store = new SaasAiPromptStore(CreateScopeFactory(repository.Object));

        var templates = await store.ListAsync();

        Assert.Equal(["alpha", "beta"], templates.Select(item => item.Name).ToList(), StringComparer.Ordinal);
    }

    /// <summary>
    /// 库里没有启用模板时必须返回空集合而不是 null。
    /// </summary>
    [Fact]
    public async Task PromptStoreListAsync_EmptyRepositoryShouldReturnEmptyList()
    {
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.GetEnabledListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var store = new SaasAiPromptStore(CreateScopeFactory(repository.Object));

        var templates = await store.ListAsync();

        Assert.NotNull(templates);
        Assert.Empty(templates);
    }

    /// <summary>
    /// provider 名为空（含纯空白）必须走"默认且启用"解析，而不是拿空串去按编码查。
    /// </summary>
    /// <param name="providerName">空白 provider 名。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProviderStoreGetAsync_BlankNameShouldResolveDefaultProvider(string? providerName)
    {
        var fixture = CreateProviderStore();
        _ = fixture.Repository
            .Setup(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiTestHelper.CreateProvider(7));

        var options = await fixture.Store.GetAsync(providerName);

        Assert.NotNull(options);
        fixture.Repository.Verify(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(
            item => item.GetEnabledByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 指定 provider 名时必须按编码取启用行，不得回落到默认 provider。
    /// </summary>
    [Fact]
    public async Task ProviderStoreGetAsync_NamedProviderShouldResolveByConfigCode()
    {
        var fixture = CreateProviderStore();
        _ = fixture.Repository
            .Setup(item => item.GetEnabledByCodeAsync("config-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiTestHelper.CreateProvider(7));

        var options = await fixture.Store.GetAsync("config-code");

        Assert.NotNull(options);
        fixture.Repository.Verify(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 无匹配配置必须返回 null，由调用方 fail-closed 处理，不得抛异常打断请求管道。
    /// </summary>
    [Fact]
    public async Task ProviderStoreGetAsync_MissingProviderShouldReturnNull()
    {
        var fixture = CreateProviderStore();
        _ = fixture.Repository
            .Setup(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiProvider?)null);

        Assert.Null(await fixture.Store.GetAsync());
    }

    /// <summary>
    /// 映射到框架选项时 Provider 键必须取 ConfigCode 而不是实体的 Provider 列。
    /// </summary>
    /// <remarks>
    /// Provider 列是"OpenAI/DeepSeek"这类分组标签，多行可重复；解析器用它当缓存键会让不同配置互相串用。
    /// </remarks>
    [Fact]
    public async Task ProviderStoreGetAsync_ProviderKeyShouldBeConfigCodeNotVendorLabel()
    {
        var entity = AiTestHelper.CreateProvider(7, "prod-openai");
        entity.Provider = "OpenAI";
        var fixture = CreateProviderStore();
        _ = fixture.Repository
            .Setup(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var options = await fixture.Store.GetAsync();

        Assert.Equal("prod-openai", options!.Provider, StringComparer.Ordinal);
    }

    /// <summary>
    /// 映射必须搬齐运行所需的全部参数，并把密文密钥解密成明文交给上游客户端。
    /// </summary>
    [Fact]
    public async Task ProviderStoreGetAsync_ShouldDecryptApiKeyAndCarryRuntimeOptions()
    {
        var entity = AiTestHelper.CreateProvider(7);
        var fixture = CreateProviderStore();
        _ = fixture.Repository
            .Setup(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var options = await fixture.Store.GetAsync();

        Assert.Equal("plain:dp:old-cipher", options!.ApiKey, StringComparer.Ordinal);
        Assert.Equal(entity.BaseUrl, options.BaseUrl, StringComparer.Ordinal);
        Assert.Equal(entity.Model, options.Model, StringComparer.Ordinal);
        Assert.Equal(entity.EmbeddingModel, options.EmbeddingModel, StringComparer.Ordinal);
        Assert.Equal(entity.MaxOutputTokens, options.MaxOutputTokens);
        Assert.Equal(entity.Temperature, options.Temperature);
        Assert.Equal(entity.TimeoutSeconds, options.TimeoutSeconds);
        Assert.Equal(entity.ExtraJson, options.ExtraJson, StringComparer.Ordinal);
        fixture.Protector.Verify(item => item.Unprotect("dp:old-cipher"), Times.Once);
    }

    /// <summary>
    /// 全表枚举必须逐条映射启用 provider。
    /// </summary>
    [Fact]
    public async Task ProviderStoreGetAllAsync_ShouldMapEveryEnabledProvider()
    {
        var fixture = CreateProviderStore();
        _ = fixture.Repository
            .Setup(item => item.GetEnabledListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([AiTestHelper.CreateProvider(1, "alpha"), AiTestHelper.CreateProvider(2, "beta")]);

        var all = await fixture.Store.GetAllAsync();

        Assert.Equal(["alpha", "beta"], all.Select(item => item.Provider).ToList(), StringComparer.Ordinal);
    }

    /// <summary>
    /// 单行密钥解密失败必须跳过该行并继续枚举，不得整表报错，更不得回退明文。
    /// </summary>
    /// <remarks>这是 fail-closed 口径：坏行宁可不可用，也不能把密文当明文送去上游。</remarks>
    [Fact]
    public async Task ProviderStoreGetAllAsync_UndecryptableRowShouldBeSkippedWithoutFallback()
    {
        var broken = AiTestHelper.CreateProvider(1, "broken");
        broken.ApiKey = "dp:corrupted";
        var healthy = AiTestHelper.CreateProvider(2, "healthy");
        var fixture = CreateProviderStore();
        _ = fixture.Protector
            .Setup(item => item.Unprotect("dp:corrupted"))
            .Throws(new InvalidOperationException("密钥解密失败"));
        _ = fixture.Repository
            .Setup(item => item.GetEnabledListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([broken, healthy]);

        var all = await fixture.Store.GetAllAsync();

        Assert.Equal(["healthy"], all.Select(item => item.Provider).ToList(), StringComparer.Ordinal);
        Assert.DoesNotContain(all, item => string.Equals(item.ApiKey, "dp:corrupted", StringComparison.Ordinal));
    }

    /// <summary>
    /// 全部行都解不开时必须返回空集合而不是抛出，让调用方按"没有可用 provider"处理。
    /// </summary>
    [Fact]
    public async Task ProviderStoreGetAllAsync_AllRowsUndecryptableShouldReturnEmptyList()
    {
        var fixture = CreateProviderStore();
        _ = fixture.Protector
            .Setup(item => item.Unprotect(It.IsAny<string?>()))
            .Throws(new InvalidOperationException("密钥解密失败"));
        _ = fixture.Repository
            .Setup(item => item.GetEnabledListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([AiTestHelper.CreateProvider(1, "a"), AiTestHelper.CreateProvider(2, "b")]);

        Assert.Empty(await fixture.Store.GetAllAsync());
    }

    /// <summary>
    /// 单条解析路径的解密失败必须与全表枚举同口径：记 Warning 后按「未配置 provider」返回 null，
    /// 不把解密异常抛进请求管道。
    /// </summary>
    /// <remarks>
    /// 回归锚点：此处原先直接调用会解密的 Map 而无 try/catch，一条密钥损坏就会让默认 provider 解析抛异常打断请求，
    /// 与同类 GetAllAsync「坏行跳过、不阻断枚举」的降级口径相互矛盾（同一份数据、两条读路径两种失败语义）。
    /// 两条路径都是 fail-closed：坏行宁可不可用，也不回退明文。
    /// </remarks>
    [Fact]
    public async Task ProviderStoreGetAsync_UndecryptableApiKeyShouldFallBackToNull()
    {
        var fixture = CreateProviderStore();
        _ = fixture.Protector
            .Setup(item => item.Unprotect(It.IsAny<string?>()))
            .Throws(new InvalidOperationException("密钥解密失败"));
        _ = fixture.Repository
            .Setup(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiTestHelper.CreateProvider(7));

        Assert.Null(await fixture.Store.GetAsync());
    }

    /// <summary>
    /// 按编码解析的路径同样不得把解密异常抛给调用方。
    /// </summary>
    [Fact]
    public async Task ProviderStoreGetAsync_NamedProviderWithUndecryptableKeyShouldFallBackToNull()
    {
        var fixture = CreateProviderStore();
        _ = fixture.Protector
            .Setup(item => item.Unprotect(It.IsAny<string?>()))
            .Throws(new InvalidOperationException("密钥解密失败"));
        _ = fixture.Repository
            .Setup(item => item.GetEnabledByCodeAsync("alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiTestHelper.CreateProvider(7, "alpha"));

        Assert.Null(await fixture.Store.GetAsync("alpha"));
    }

    /// <summary>
    /// 取消令牌必须透传给作用域内的仓储调用。
    /// </summary>
    [Fact]
    public async Task ProviderStore_ShouldForwardCancellationTokenToRepository()
    {
        var fixture = CreateProviderStore();
        using var source = new CancellationTokenSource();
        _ = fixture.Repository
            .Setup(item => item.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiTestHelper.CreateProvider(7));
        _ = fixture.Repository
            .Setup(item => item.GetEnabledListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _ = await fixture.Store.GetAsync(cancellationToken: source.Token);
        _ = await fixture.Store.GetAllAsync(source.Token);

        fixture.Repository.Verify(item => item.GetDefaultAsync(source.Token), Times.Once);
        fixture.Repository.Verify(item => item.GetEnabledListAsync(source.Token), Times.Once);
    }

    /// <summary>
    /// RAG 配置节名一经上线不得改动，改了 appsettings 里的配置会整节失效并静默回落默认值。
    /// </summary>
    [Fact]
    public void RagOptions_SectionNameShouldBeStable()
    {
        Assert.Equal("XiHan:AI:Rag", XiHanRagOptions.SectionName, StringComparer.Ordinal);
    }

    /// <summary>
    /// RAG 默认值必须是"本机 Qdrant + 明文 gRPC + 无鉴权"这一套开箱可跑的开发配置。
    /// </summary>
    [Fact]
    public void RagOptions_DefaultsShouldTargetLocalQdrantOverPlainGrpc()
    {
        var options = new XiHanRagOptions();

        Assert.Equal("localhost", options.QdrantHost, StringComparer.Ordinal);
        Assert.Equal(6334, options.QdrantPort);
        Assert.False(options.QdrantHttps);
        Assert.Null(options.QdrantApiKey);
    }

    /// <summary>
    /// 检索默认条数与集合名/向量维度必须同源于框架常量，脱钩会出现"按常量建集合、按默认值写入"的错配。
    /// </summary>
    [Fact]
    public void RagOptions_CollectionAndDimensionsShouldFollowFrameworkDefaults()
    {
        var options = new XiHanRagOptions();

        Assert.Equal(5, options.DefaultTopK);
        Assert.Equal(KnowledgeVectorOptions.DefaultCollectionName, options.CollectionName, StringComparer.Ordinal);
        Assert.Equal(KnowledgeVectorOptions.DefaultDimensions, options.EmbeddingDimensions);
    }

    /// <summary>
    /// RAG 配置的每一项都必须可写，否则 appsettings 绑定会静默失效、线上仍连本机 Qdrant。
    /// </summary>
    [Fact]
    public void RagOptions_EveryPropertyShouldBeBindable()
    {
        var options = new XiHanRagOptions
        {
            QdrantHost = "vector.internal",
            QdrantPort = 6335,
            QdrantHttps = true,
            QdrantApiKey = "qk-1",
            DefaultTopK = 9,
            CollectionName = "custom_collection",
            EmbeddingDimensions = 1024
        };

        Assert.Equal("vector.internal", options.QdrantHost, StringComparer.Ordinal);
        Assert.Equal(6335, options.QdrantPort);
        Assert.True(options.QdrantHttps);
        Assert.Equal("qk-1", options.QdrantApiKey, StringComparer.Ordinal);
        Assert.Equal(9, options.DefaultTopK);
        Assert.Equal("custom_collection", options.CollectionName, StringComparer.Ordinal);
        Assert.Equal(1024, options.EmbeddingDimensions);
    }

    /// <summary>
    /// 构造承载 Scoped 仓储替身的真实 DI 作用域工厂。
    /// </summary>
    /// <typeparam name="TRepository">仓储契约类型。</typeparam>
    /// <param name="repository">仓储替身实例。</param>
    /// <returns>作用域工厂。</returns>
    private static IServiceScopeFactory CreateScopeFactory<TRepository>(TRepository repository)
        where TRepository : class
    {
        var services = new ServiceCollection();
        _ = services.AddScoped(_ => repository);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// 构造 provider 配置源夹具：解密器把密文加上可辨识前缀，便于断言"确实解过密"。
    /// </summary>
    /// <returns>被测配置源与其依赖替身。</returns>
    private static ProviderStoreFixture CreateProviderStore()
    {
        var repository = new Mock<IAiProviderRepository>(MockBehavior.Strict);
        var protector = new Mock<IAiProviderSecretProtector>(MockBehavior.Strict);
        _ = protector
            .Setup(item => item.Unprotect(It.IsAny<string?>()))
            .Returns((string? cipher) => cipher is null ? null : "plain:" + cipher);

        var store = new SaasAiProviderConfigStore(
            CreateScopeFactory(repository.Object),
            protector.Object,
            NullLogger<SaasAiProviderConfigStore>.Instance);

        return new ProviderStoreFixture(store, repository, protector);
    }

    /// <summary>
    /// provider 配置源测试夹具。
    /// </summary>
    /// <param name="Store">被测配置源。</param>
    /// <param name="Repository">仓储替身。</param>
    /// <param name="Protector">密钥保护器替身。</param>
    private sealed record ProviderStoreFixture(
        SaasAiProviderConfigStore Store,
        Mock<IAiProviderRepository> Repository,
        Mock<IAiProviderSecretProtector> Protector);
}
