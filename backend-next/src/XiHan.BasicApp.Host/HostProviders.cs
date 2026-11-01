// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Caching.Redis;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Messaging.RabbitMQ;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 按配置装配的分布式 Provider。
/// </summary>
/// <remarks>
/// 方案 5.13：配了连接才装 Redis（连同两级缓存）与 RabbitMQ，没配即 Full Profile 的内存实现，单机零外部依赖。
/// 连接的来源与框架 Provider 读取时同一套：显式的 <c>XiHan:Caching:Redis:Configuration</c> / <c>XiHan:Messaging:RabbitMQ:Configuration</c>，
/// 或绑定 <c>cache</c> / <c>queue</c>（<c>XiHan:Bindings</c> 与 <c>SERVICE_BINDING_ROOT</c> 目录）；值为空白即没配。
/// 配了而连不上时由 Provider 在启动时报错中断，不退回内存实现。
/// </remarks>
public sealed record HostProviders
{
    /// <summary>
    /// 是否装配 Redis 共享缓存与两级缓存。
    /// </summary>
    public bool Redis { get; init; }

    /// <summary>
    /// 是否装配 RabbitMQ 消息代理。
    /// </summary>
    public bool RabbitMQ { get; init; }

    /// <summary>
    /// 从配置与绑定判定要装配哪些分布式 Provider。
    /// </summary>
    /// <param name="configuration">宿主配置（调用方改完配置之后）。</param>
    public static HostProviders FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        // 与 AddXiHan 解析绑定用同一个解析器：配置节与 Service Binding 目录都算
        var bindings = BindingResolver.Resolve(configuration);
        return new HostProviders
        {
            Redis = Configured(configuration, RedisCachingOptions.ConfigurationSection, bindings, RedisCachingFeature.ConnectionBinding),
            RabbitMQ = Configured(configuration, RabbitMQMessagingOptions.ConfigurationSection, bindings, RabbitMQMessagingFeature.ConnectionBinding),
        };
    }

    /// <summary>
    /// 判断某个 Provider 的连接是否配了。
    /// </summary>
    /// <remarks>
    /// 显式配置节的 <c>Configuration</c> 或同名绑定的值，任一不是空白即算配了。
    /// </remarks>
    private static bool Configured(IConfiguration configuration, string section, IReadOnlyDictionary<string, BindingValue> bindings, string binding)
        => !string.IsNullOrWhiteSpace(configuration[$"{section}:Configuration"])
           || (bindings.TryGetValue(binding, out var value) && !string.IsNullOrWhiteSpace(value.Value));
}
