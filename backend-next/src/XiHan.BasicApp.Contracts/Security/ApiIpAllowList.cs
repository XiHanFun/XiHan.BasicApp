// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 开放接口的 IP 白名单。
/// </summary>
/// <remarks>
/// 每条是一个 IP（v4 / v6）或 CIDR 网段。Identity 保存时校验，OpenPlatform 验签时匹配，两边同一套规则。
/// </remarks>
public static class ApiIpAllowList
{
    /// <summary>
    /// 最多条数。
    /// </summary>
    public const int MaxEntries = 50;

    /// <summary>
    /// 解析一条白名单。
    /// </summary>
    /// <remarks>
    /// 单个地址按全长前缀的网段处理；IPv4 映射的 IPv6 还原为 IPv4。
    /// </remarks>
    /// <param name="entry">一条。</param>
    /// <param name="network">网段。</param>
    /// <returns>是否合法。</returns>
    public static bool TryParse(string? entry, out IPNetwork network)
    {
        network = default;
        var text = entry?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.Contains('/', StringComparison.Ordinal))
        {
            return IPNetwork.TryParse(text, out network);
        }

        if (!IPAddress.TryParse(text, out var address))
        {
            return false;
        }

        address = Normalize(address);
        network = new IPNetwork(address, address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128);
        return true;
    }

    /// <summary>
    /// 规整一份白名单（按行或逗号分隔，去空去重）。
    /// </summary>
    /// <remarks>
    /// 有一条不合法即失败并给出那一条。
    /// </remarks>
    /// <param name="value">原文。</param>
    /// <param name="entries">规整后的各条。</param>
    /// <param name="invalid">不合法的那一条。</param>
    /// <returns>是否合法。</returns>
    public static bool TryNormalize(string? value, out IReadOnlyList<string> entries, [NotNullWhen(false)] out string? invalid)
    {
        var result = new List<string>();
        foreach (var raw in Split(value))
        {
            if (!TryParse(raw, out var network))
            {
                entries = [];
                invalid = raw;
                return false;
            }

            var text = network.PrefixLength == (network.BaseAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128)
                ? network.BaseAddress.ToString()
                : network.ToString();
            if (!result.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(text);
            }
        }

        entries = result;
        invalid = null;
        return true;
    }

    /// <summary>
    /// 判断地址是否在白名单内。
    /// </summary>
    /// <remarks>
    /// 白名单为空即不限。解析不出的条目不放行任何地址。
    /// </remarks>
    /// <param name="entries">白名单。</param>
    /// <param name="remoteAddress">客户端地址。</param>
    /// <returns>是否放行。</returns>
    public static bool Allows(IReadOnlyList<string> entries, string? remoteAddress)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            return true;
        }

        if (!IPAddress.TryParse(remoteAddress, out var address))
        {
            return false;
        }

        address = Normalize(address);
        return entries.Any(entry => TryParse(entry, out var network) && network.Contains(address));
    }

    /// <summary>
    /// 按换行或逗号拆开，去掉空白。
    /// </summary>
    /// <param name="value">原文。</param>
    /// <returns>各条。</returns>
    public static IReadOnlyList<string> Split(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
