#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsGatewayResolver
// Guid:0a6e3c91-5d27-4f84-b0c6-8e1f4a7d2b59
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 短信网关解析器实现
/// </summary>
/// <remarks>
/// 镜像 <c>StorageProviderResolver</c> 范式：每次解析都读 DB「默认且启用」配置行（含租户过滤），
/// 网关客户端按配置指纹缓存复用——指纹变化即重建，改配置即热生效。
/// </remarks>
public sealed class SmsGatewayResolver : ISmsGatewayResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISmsConfigSecretProtector _secretProtector;

    private readonly ConcurrentDictionary<long, CachedClient> _cache = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsGatewayResolver(
        IServiceScopeFactory scopeFactory,
        ISmsConfigSecretProtector secretProtector)
    {
        _scopeFactory = scopeFactory;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<ISmsGatewayClient?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        SysSmsConfig? config;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ISmsConfigRepository>();
            config = await repository.GetDefaultAsync(cancellationToken);
        }

        if (config is null)
        {
            return null;
        }

        var fingerprint = Fingerprint(config);
        var cached = _cache.AddOrUpdate(
            config.BasicId,
            _ => new CachedClient(fingerprint, Build(config)),
            (_, existing) => existing.Fingerprint == fingerprint ? existing : new CachedClient(fingerprint, Build(config)));
        return cached.Client;
    }

    private static string Fingerprint(SysSmsConfig config)
    {
        return string.Join('|',
            config.BasicId, (int)config.Provider, config.AccessKeyId, config.AccessKeySecret,
            config.SdkAppId, config.SignName, config.Region, config.TemplateMap, config.IsEnabled);
    }

    private ISmsGatewayClient Build(SysSmsConfig config)
    {
        var secret = _secretProtector.Unprotect(config.AccessKeySecret)
            ?? throw new InvalidOperationException("短信网关访问密钥为空。");

        return config.Provider switch
        {
            SmsProviderType.Aliyun => new AliyunSmsGatewayClient(
                config.AccessKeyId, secret, config.SignName, config.TemplateMap),
            SmsProviderType.TencentCloud => new TencentCloudSmsGatewayClient(
                config.AccessKeyId, secret,
                config.SdkAppId ?? throw new InvalidOperationException("腾讯云短信配置缺少应用ID（SmsSdkAppId）。"),
                config.Region ?? throw new InvalidOperationException("腾讯云短信配置缺少地域。"),
                config.SignName, config.TemplateMap),
            _ => throw new InvalidOperationException($"不支持的短信服务商：{config.Provider}。")
        };
    }

    private sealed record CachedClient(string Fingerprint, ISmsGatewayClient Client);
}
