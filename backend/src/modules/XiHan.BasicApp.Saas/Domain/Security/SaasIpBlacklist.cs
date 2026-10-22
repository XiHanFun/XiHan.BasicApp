// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Configurations;

namespace XiHan.BasicApp.Saas.Domain.Security;

/// <summary>
/// IP 黑名单：由参数 <see cref="SaasConfigKeys.Security.IpBlacklist"/> 的条目解析出的地址网段集合。
/// </summary>
/// <remarks>
/// 条目是单个 IPv4 / IPv6 地址或 CIDR 网段；IPv4 只认四段点分写法，网段的主机位必须为零。
/// 匹配前把 IPv4 映射的 IPv6 地址（::ffff:a.b.c.d）还原成 IPv4。实例不可变，可跨请求共用。
/// </remarks>
public sealed class SaasIpBlacklist
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPNetwork[] _networks;

    private SaasIpBlacklist(IPNetwork[] networks, IReadOnlyList<string> invalidEntries)
    {
        _networks = networks;
        InvalidEntries = invalidEntries;
    }

    /// <summary>
    /// 空黑名单：不拒绝任何地址。
    /// </summary>
    public static SaasIpBlacklist Empty { get; } = new([], []);

    /// <summary>
    /// 有效网段数。
    /// </summary>
    public int Count => _networks.Length;

    /// <summary>
    /// 解析不了的条目（原样去掉首尾空白）。
    /// </summary>
    public IReadOnlyList<string> InvalidEntries { get; }

    /// <summary>
    /// 从参数值（<see cref="SaasIpBlacklistSettings"/> 的 JSON）解析黑名单；值为空白时为空黑名单。
    /// </summary>
    /// <param name="json">参数值。</param>
    /// <returns>解析出的黑名单，解析不了的条目记在 <see cref="InvalidEntries"/>。</returns>
    /// <exception cref="InvalidOperationException">参数值不是合法的 <see cref="SaasIpBlacklistSettings"/> JSON。</exception>
    public static SaasIpBlacklist FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty;
        }

        SaasIpBlacklistSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<SaasIpBlacklistSettings>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"IP 黑名单不是合法的 JSON：{exception.Message}", exception);
        }

        return Parse(settings?.Entries);
    }

    /// <summary>
    /// 从条目解析黑名单；空白条目跳过。
    /// </summary>
    /// <param name="entries">单个地址或 CIDR 网段。</param>
    /// <returns>解析出的黑名单，解析不了的条目记在 <see cref="InvalidEntries"/>。</returns>
    public static SaasIpBlacklist Parse(IEnumerable<string>? entries)
    {
        var networks = new List<IPNetwork>();
        var invalid = new List<string>();
        foreach (var raw in entries ?? [])
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var entry = raw.Trim();
            if (TryParseEntry(entry, out var network))
            {
                networks.Add(network);
            }
            else
            {
                invalid.Add(entry);
            }
        }

        return networks.Count == 0 && invalid.Count == 0 ? Empty : new SaasIpBlacklist([.. networks], invalid);
    }

    /// <summary>
    /// 解析一个条目：单个地址按主机网段（/32、/128）处理。
    /// </summary>
    /// <param name="entry">单个地址或 CIDR 网段。</param>
    /// <param name="network">成功时返回网段。</param>
    /// <returns>是合法地址或网段时返回 <see langword="true"/>。</returns>
    public static bool TryParseEntry(string entry, out IPNetwork network)
    {
        network = default;
        var slash = entry.IndexOf('/');
        var addressText = slash < 0 ? entry : entry[..slash];
        if (!TryParseAddress(addressText, out var address))
        {
            return false;
        }

        var maxPrefix = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (slash < 0)
        {
            network = new IPNetwork(address, maxPrefix);
            return true;
        }

        if (!int.TryParse(entry.AsSpan(slash + 1), System.Globalization.NumberStyles.None, null, out var prefix) || prefix > maxPrefix)
        {
            return false;
        }

        // 主机位不为零的网段（如 203.0.113.5/24）多半是笔误，不替使用者猜是哪个网段
        if (!HostBitsAreZero(address, prefix))
        {
            return false;
        }

        network = new IPNetwork(address, prefix);
        return true;
    }

    /// <summary>
    /// 地址是否落在黑名单里。
    /// </summary>
    /// <param name="address">客户端地址；为空时不命中。</param>
    /// <returns>命中任一网段时返回 <see langword="true"/>。</returns>
    public bool Contains(IPAddress? address)
    {
        if (address is null || _networks.Length == 0)
        {
            return false;
        }

        var normalized = Normalize(address);
        foreach (var network in _networks)
        {
            if (network.Contains(normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseAddress(string text, out IPAddress address)
    {
        address = IPAddress.None;
        // IPAddress.TryParse 认 "10.1"、"203.0.113" 这类简写，会落到意料之外的地址；IPv4 只收四段点分，IPv6 不收区域标识
        if (text.Length == 0 || text.Contains('%')
            || (!text.Contains(':') && text.Count(static ch => ch == '.') != 3)
            || !IPAddress.TryParse(text, out var parsed))
        {
            return false;
        }

        address = Normalize(parsed);
        return true;
    }

    private static bool HostBitsAreZero(IPAddress address, int prefix)
    {
        var bytes = address.GetAddressBytes();
        for (var bit = prefix; bit < bytes.Length * 8; bit++)
        {
            if ((bytes[bit / 8] & (0x80 >> (bit % 8))) != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static IPAddress Normalize(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }
}
