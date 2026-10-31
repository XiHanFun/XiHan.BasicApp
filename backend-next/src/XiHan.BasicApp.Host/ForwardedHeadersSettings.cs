// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 反向代理转发头 <c>BasicApp:ForwardedHeaders</c>。
/// </summary>
/// <remarks>
/// 宿主在管道最前面还原 <c>X-Forwarded-For</c> / <c>-Proto</c> / <c>-Host</c>：访问日志、会话与登录地点、登录节流与自动封禁、访问策略的地址与地区条件拿到的是客户端地址，而不是代理的地址（照 4.x 的 <c>Api:ForwardedHeaders</c>）。
/// 只信本机回环与这里列出的代理（<c>KnownProxies</c>，单个地址）和网段（<c>KnownNetworks</c>，CIDR 且主机位为 0）；<c>ForwardLimit</c> 是从右往左认几层转发，缺省 1。配置写错启动即报错。
/// </remarks>
public sealed record ForwardedHeadersSettings
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:ForwardedHeaders";

    /// <summary>
    /// 受信的代理地址。
    /// </summary>
    public IReadOnlyList<IPAddress> KnownProxies { get; init; } = [];

    /// <summary>
    /// 受信的代理网段。
    /// </summary>
    public IReadOnlyList<System.Net.IPNetwork> KnownNetworks { get; init; } = [];

    /// <summary>
    /// 认几层转发。
    /// </summary>
    public int ForwardLimit { get; init; } = 1;

    /// <summary>
    /// 从配置读取转发头设置。
    /// </summary>
    /// <remarks>
    /// 没写即只信本机回环、认一层；地址、网段或层数写错抛出 <see cref="ArgumentException"/>。
    /// </remarks>
    public static ForwardedHeadersSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var proxies = new List<IPAddress>();
        foreach (var value in Values(section, nameof(KnownProxies)))
        {
            proxies.Add(IPAddress.TryParse(value, out var address)
                ? address
                : throw new ArgumentException($"'{ConfigurationSection}:{nameof(KnownProxies)}' contains '{value}', which is not an IP address."));
        }

        var networks = new List<System.Net.IPNetwork>();
        foreach (var value in Values(section, nameof(KnownNetworks)))
        {
            networks.Add(TryParseNetwork(value, out var network)
                ? network
                : throw new ArgumentException($"'{ConfigurationSection}:{nameof(KnownNetworks)}' contains '{value}', which is not a CIDR network with a zero host part; put single addresses in {nameof(KnownProxies)}."));
        }

        var limit = 1;
        var rawLimit = section[nameof(ForwardLimit)];
        if (!string.IsNullOrEmpty(rawLimit) && (!int.TryParse(rawLimit, out limit) || limit < 1))
        {
            throw new ArgumentException($"'{ConfigurationSection}:{nameof(ForwardLimit)}' must be a positive integer, but was '{rawLimit}'.");
        }

        return new ForwardedHeadersSettings { KnownProxies = proxies, KnownNetworks = networks, ForwardLimit = limit };
    }

    /// <summary>
    /// 写进 ASP.NET Core 的转发头选项。
    /// </summary>
    /// <remarks>
    /// 受信列表追加在框架缺省的本机回环之后。
    /// </remarks>
    public void Apply(ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.ForwardLimit = ForwardLimit;
        foreach (var proxy in KnownProxies)
        {
            options.KnownProxies.Add(proxy);
        }

        foreach (var network in KnownNetworks)
        {
            options.KnownIPNetworks.Add(network);
        }
    }

    private static IEnumerable<string> Values(IConfigurationSection section, string key)
        => section.GetSection(key).GetChildren().Select(static child => child.Value?.Trim()).Where(static value => !string.IsNullOrEmpty(value))!;

    private static bool TryParseNetwork(string value, out System.Net.IPNetwork network)
    {
        var slash = value.IndexOf('/', StringComparison.Ordinal);
        return System.Net.IPNetwork.TryParse(value, out network)
            && slash > 0
            && IPAddress.TryParse(value[..slash], out var address)
            && address.Equals(network.BaseAddress);
    }
}
