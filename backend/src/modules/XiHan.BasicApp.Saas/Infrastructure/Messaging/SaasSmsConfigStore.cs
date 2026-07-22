// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Bot.Sms.Abstractions;
using XiHan.Framework.Bot.Sms.Options;
using BotSmsProviderType = XiHan.Framework.Bot.Sms.Enums.SmsProviderType;
using SaasSmsProviderType = XiHan.BasicApp.Saas.Domain.Entities.SmsProviderType;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS 短信配置存储（数据库实现，覆盖框架默认空实现）
/// </summary>
/// <remarks>
/// 每次读取「默认且启用」的 <c>SysSmsConfig</c>（含租户过滤）并解密访问密钥，
/// 映射为框架自有模型 <see cref="SmsChannelConfig"/>（AccessKeySecret 为已解密明文）。
/// Singleton 生命周期，Scoped 仓储经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public sealed class SaasSmsConfigStore : ISmsConfigStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISmsConfigSecretProtector _secretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 仓储）</param>
    /// <param name="secretProtector">短信网关访问密钥保护器</param>
    public SaasSmsConfigStore(
        IServiceScopeFactory scopeFactory,
        ISmsConfigSecretProtector secretProtector)
    {
        _scopeFactory = scopeFactory;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<SmsChannelConfig?> GetAsync(CancellationToken cancellationToken = default)
    {
        Domain.Entities.SysSmsConfig? config;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ISmsConfigRepository>();
            config = await repository.GetDefaultAsync(cancellationToken);
        }

        if (config is null)
        {
            return null;
        }

        var secret = _secretProtector.Unprotect(config.AccessKeySecret)
            ?? throw new InvalidOperationException("短信网关访问密钥为空。");

        return new SmsChannelConfig
        {
            ConfigId = config.BasicId,
            Provider = MapProvider(config.Provider),
            AccessKeyId = config.AccessKeyId,
            AccessKeySecret = secret,
            SdkAppId = config.SdkAppId,
            SignName = config.SignName,
            Region = config.Region,
            TemplateMap = config.TemplateMap,
            IsEnabled = config.IsEnabled
        };
    }

    private static BotSmsProviderType MapProvider(SaasSmsProviderType provider)
    {
        return provider switch
        {
            SaasSmsProviderType.Aliyun => BotSmsProviderType.Aliyun,
            SaasSmsProviderType.TencentCloud => BotSmsProviderType.TencentCloud,
            _ => throw new InvalidOperationException($"不支持的短信服务商：{provider}。")
        };
    }
}
