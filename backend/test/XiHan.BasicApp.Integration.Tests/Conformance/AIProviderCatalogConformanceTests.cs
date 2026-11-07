// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.AI.Conformance.Providers;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests.Conformance;

/// <summary>
/// 落库的 Provider 目录跑框架的 Provider 目录一致性套件
/// </summary>
/// <remarks>
/// 方案 A1 的自证：库里一行提供商指向仿真的 OpenAI 兼容端点（照框架 OpenAI 一致性套件的做法），目录按编码解析聊天与嵌入、缓存到失效为止、未知名字即 <c>AI_NOTFOUND_001</c>
/// </remarks>
public sealed class AIProviderCatalogConformanceTests : AIProviderConformance, IDisposable
{
    private const string ProviderName = "conformance";

    private readonly SqliteDatabase _database = new();

    private readonly FakeOpenAIHandler _model = new();

    public void Dispose()
    {
        _database.Dispose();
        _model.Dispose();
    }

    /// <inheritdoc />
    protected override Task<AIProviderHost> StartAsync() => StartAsync(isDefault: true);

    /// <inheritdoc />
    protected override Task<AIProviderHost> StartWithoutDefaultAsync() => StartAsync(isDefault: false);

    private async Task<AIProviderHost> StartAsync(bool isDefault)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: _model.Attach);
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var secrets = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
            var added = await server.InvokeAsync(async ctx => await ctx.Services.GetRequiredService<IRepository<SysAiProvider, Guid>>().AddAsync(
                new SysAiProvider { Id = Guid.NewGuid(), Code = ProviderName, IsDefault = isDefault, Name = "Conformance", Vendor = "OpenAI", ChatModel = "chat-model", EmbeddingModel = "embedding-model", Endpoint = FakeOpenAIHandler.Endpoint, ApiKey = secrets.Protect(AiProviderService.ApiKeyPurpose, "sk-test") },
                ctx.CancellationToken));
            Assert.True(added.IsSuccess);
        }

        await server.Services.GetRequiredService<AiProviderCatalog>().ReloadAsync(CancellationToken.None);
        return new AIProviderHost(server.Services, ProviderName, _model.Dimensions, server);
    }
}
