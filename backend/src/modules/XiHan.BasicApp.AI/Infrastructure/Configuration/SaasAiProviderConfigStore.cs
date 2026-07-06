#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasAiProviderConfigStore
// Guid:83e36abc-c6a4-4b63-bb2a-60b2a25753cf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.Framework.AI.Abstractions.Configuration;

namespace XiHan.BasicApp.AI.Infrastructure.Configuration;

/// <summary>
/// SaaS AI Provider 配置存储（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 每次读取 <c>SysAiProvider</c>（含租户过滤）并解密 ApiKey，映射为框架 <see cref="AiProviderOptions"/>。
/// providerName 为空取「默认且启用」行；否则按 ConfigCode 取启用行；无匹配返回 null（提供者按未配置处理）。
/// Singleton 生命周期，Scoped 仓储经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// 不做进程内缓存：解析器（IAiChatClientResolver）已缓存构建好的 IChatClient，写入后经 Invalidate 失效重建。
/// </remarks>
public sealed class SaasAiProviderConfigStore : IAiProviderConfigStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiProviderSecretProtector _secretProtector;
    private readonly ILogger<SaasAiProviderConfigStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasAiProviderConfigStore(
        IServiceScopeFactory scopeFactory,
        IAiProviderSecretProtector secretProtector,
        ILogger<SaasAiProviderConfigStore> logger)
    {
        _scopeFactory = scopeFactory;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiProviderOptions?> GetAsync(string? providerName = null, CancellationToken cancellationToken = default)
    {
        SysAiProvider? provider;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAiProviderRepository>();
            provider = string.IsNullOrWhiteSpace(providerName)
                ? await repository.GetDefaultAsync(cancellationToken)
                : await repository.GetEnabledByCodeAsync(providerName, cancellationToken);
        }

        return provider is null ? null : Map(provider);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiProviderOptions>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SysAiProvider> providers;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAiProviderRepository>();
            providers = await repository.GetEnabledListAsync(cancellationToken);
        }

        var list = new List<AiProviderOptions>(providers.Count);
        foreach (var provider in providers)
        {
            try
            {
                list.Add(Map(provider));
            }
            catch (Exception ex)
            {
                // 单行解密失败不阻断全表枚举（fail-closed：跳过坏行，不回退明文）
                _logger.LogWarning(ex, "AI Provider [{ConfigCode}] 密钥解密失败，已跳过。", provider.ConfigCode);
            }
        }

        return list;
    }

    /// <summary>
    /// 实体 → 框架配置选项（解密 ApiKey；Provider 键用 ConfigCode，供解析器缓存）
    /// </summary>
    private AiProviderOptions Map(SysAiProvider provider)
    {
        return new AiProviderOptions
        {
            Provider = provider.ConfigCode,
            ApiKey = _secretProtector.Unprotect(provider.ApiKey),
            BaseUrl = provider.BaseUrl,
            Model = provider.Model,
            EmbeddingModel = provider.EmbeddingModel,
            MaxOutputTokens = provider.MaxOutputTokens,
            Temperature = provider.Temperature,
            TimeoutSeconds = provider.TimeoutSeconds,
            ExtraJson = provider.ExtraJson
        };
    }
}
