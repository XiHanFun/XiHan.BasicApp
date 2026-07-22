// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Bot.DingTalk.Abstractions;
using XiHan.Framework.Bot.DingTalk.Options;
using XiHan.Framework.Bot.Lark.Abstractions;
using XiHan.Framework.Bot.Lark.Options;
using XiHan.Framework.Bot.WeCom.Abstractions;
using XiHan.Framework.Bot.WeCom.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS Webhook 型机器人配置存储公共基座（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 每次读取指定服务商「默认且启用」的 <c>SysBotConfig</c>（含租户过滤）并解密签名秘钥；
/// 实体存的是含凭证的完整 Webhook 地址，映射时按各家 Bot 客户端的拼接规则反向拆解
/// （钉钉 <c>?access_token=</c>、飞书 <c>/hook/{token}</c>、企微 <c>?key=</c>），
/// 拆解失败按未配置处理（返回 null，fail-closed）并记日志。
/// Singleton 生命周期，Scoped 仓储经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public abstract class SaasWebhookBotConfigStoreBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 仓储）</param>
    /// <param name="secretProtector">机器人配置签名秘钥保护器</param>
    protected SaasWebhookBotConfigStoreBase(
        IServiceScopeFactory scopeFactory,
        IBotConfigSecretProtector secretProtector)
    {
        ScopeFactory = scopeFactory;
        SecretProtector = secretProtector;
    }

    /// <summary>
    /// 服务作用域工厂
    /// </summary>
    protected IServiceScopeFactory ScopeFactory { get; }

    /// <summary>
    /// 签名秘钥保护器
    /// </summary>
    protected IBotConfigSecretProtector SecretProtector { get; }

    /// <summary>
    /// 读取指定服务商「默认且启用」的机器人配置行
    /// </summary>
    protected async Task<SysBotConfig?> GetDefaultConfigAsync(BotProviderType provider, CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBotConfigRepository>();
        return await repository.GetDefaultAsync(provider, cancellationToken);
    }

    /// <summary>
    /// 从完整 Webhook 地址中拆出查询串凭证参数（钉钉 access_token / 企微 key）
    /// </summary>
    /// <param name="webhookUrl">完整 Webhook 地址</param>
    /// <param name="parameterName">凭证参数名</param>
    /// <param name="baseUrl">去掉查询串的基础地址</param>
    /// <param name="credential">凭证参数值（已解码）</param>
    /// <returns>是否拆解成功</returns>
    protected static bool TryExtractQueryCredential(string webhookUrl, string parameterName, out string baseUrl, out string credential)
    {
        baseUrl = string.Empty;
        credential = string.Empty;
        if (!Uri.TryCreate(webhookUrl?.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        baseUrl = uri.GetLeftPart(UriPartial.Path);
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = Uri.UnescapeDataString(pair[..separatorIndex]);
            if (!string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            credential = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            return !string.IsNullOrWhiteSpace(credential);
        }

        return false;
    }

    /// <summary>
    /// 从完整 Webhook 地址中拆出末段路径凭证（飞书 /hook/{token}）
    /// </summary>
    /// <param name="webhookUrl">完整 Webhook 地址</param>
    /// <param name="baseUrl">去掉末段的基础地址</param>
    /// <param name="credential">末段凭证（已解码）</param>
    /// <returns>是否拆解成功</returns>
    protected static bool TryExtractTrailingPathCredential(string webhookUrl, out string baseUrl, out string credential)
    {
        baseUrl = string.Empty;
        credential = string.Empty;
        if (!Uri.TryCreate(webhookUrl?.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        var separatorIndex = path.LastIndexOf('/');
        if (separatorIndex <= 0)
        {
            return false;
        }

        credential = Uri.UnescapeDataString(path[(separatorIndex + 1)..]);
        baseUrl = uri.GetLeftPart(UriPartial.Authority) + path[..separatorIndex];
        return !string.IsNullOrWhiteSpace(credential);
    }

    /// <summary>
    /// 解密签名秘钥（无秘钥返回空串，匹配框架 Options 的非空字符串语义）
    /// </summary>
    protected string UnprotectSecret(SysBotConfig config)
    {
        return SecretProtector.Unprotect(config.Secret) ?? string.Empty;
    }
}

/// <summary>
/// SaaS 钉钉配置存储（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 钉钉 Bot 客户端按 <c>WebHookUrl + "?access_token=" + AccessToken</c> 拼接请求地址，
/// 故从实体完整 Webhook 地址反解 access_token；Secret 为加签秘钥、KeyWord 为安全关键词。
/// </remarks>
public sealed class SaasDingTalkConfigStore : SaasWebhookBotConfigStoreBase, IDingTalkConfigStore
{
    private readonly ILogger<SaasDingTalkConfigStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasDingTalkConfigStore(
        IServiceScopeFactory scopeFactory,
        IBotConfigSecretProtector secretProtector,
        ILogger<SaasDingTalkConfigStore> logger)
        : base(scopeFactory, secretProtector)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DingTalkOptions?> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetDefaultConfigAsync(BotProviderType.DingTalk, cancellationToken);
        if (config is null)
        {
            return null;
        }

        if (!TryExtractQueryCredential(config.WebhookUrl, "access_token", out var baseUrl, out var accessToken))
        {
            _logger.LogWarning("钉钉机器人配置 Webhook 地址缺少 access_token 参数，按未配置处理。ConfigCode={ConfigCode}", config.ConfigCode);
            return null;
        }

        return new DingTalkOptions
        {
            Enabled = true,
            WebHookUrl = baseUrl,
            AccessToken = accessToken,
            Secret = UnprotectSecret(config),
            KeyWord = config.Keyword
        };
    }
}

/// <summary>
/// SaaS 飞书配置存储（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 飞书 Bot 客户端按 <c>WebHookUrl + "/" + AccessToken</c> 拼接请求地址，
/// 故从实体完整 Webhook 地址（形如 .../hook/{token}）反解末段 token；Secret 为签名秘钥。
/// </remarks>
public sealed class SaasLarkConfigStore : SaasWebhookBotConfigStoreBase, ILarkConfigStore
{
    private readonly ILogger<SaasLarkConfigStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasLarkConfigStore(
        IServiceScopeFactory scopeFactory,
        IBotConfigSecretProtector secretProtector,
        ILogger<SaasLarkConfigStore> logger)
        : base(scopeFactory, secretProtector)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LarkOptions?> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetDefaultConfigAsync(BotProviderType.Lark, cancellationToken);
        if (config is null)
        {
            return null;
        }

        if (!TryExtractTrailingPathCredential(config.WebhookUrl, out var baseUrl, out var accessToken))
        {
            _logger.LogWarning("飞书机器人配置 Webhook 地址缺少末段 token，按未配置处理。ConfigCode={ConfigCode}", config.ConfigCode);
            return null;
        }

        return new LarkOptions
        {
            Enabled = true,
            WebHookUrl = baseUrl,
            AccessToken = accessToken,
            Secret = UnprotectSecret(config),
            KeyWord = config.Keyword
        };
    }
}

/// <summary>
/// SaaS 企业微信配置存储（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 企微 Bot 客户端按 <c>WebHookUrl + "?key=" + Key</c> 拼接请求地址，
/// 故从实体完整 Webhook 地址反解 key；上传地址随基础地址推导（.../send → .../upload_media），
/// 非标准地址保留框架默认值。企微无独立签名秘钥（Secret 不使用）。
/// </remarks>
public sealed class SaasWeComConfigStore : SaasWebhookBotConfigStoreBase, IWeComConfigStore
{
    private readonly ILogger<SaasWeComConfigStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasWeComConfigStore(
        IServiceScopeFactory scopeFactory,
        IBotConfigSecretProtector secretProtector,
        ILogger<SaasWeComConfigStore> logger)
        : base(scopeFactory, secretProtector)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WeComOptions?> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetDefaultConfigAsync(BotProviderType.WeCom, cancellationToken);
        if (config is null)
        {
            return null;
        }

        if (!TryExtractQueryCredential(config.WebhookUrl, "key", out var baseUrl, out var key))
        {
            _logger.LogWarning("企业微信机器人配置 Webhook 地址缺少 key 参数，按未配置处理。ConfigCode={ConfigCode}", config.ConfigCode);
            return null;
        }

        var options = new WeComOptions
        {
            Enabled = true,
            WebHookUrl = baseUrl,
            Key = key
        };

        // 上传地址与发送地址同源：标准形态 .../webhook/send → .../webhook/upload_media；非标准形态保留框架默认
        if (baseUrl.EndsWith("/send", StringComparison.OrdinalIgnoreCase))
        {
            options.UploadUrl = baseUrl[..^"/send".Length] + "/upload_media";
        }

        return options;
    }
}
