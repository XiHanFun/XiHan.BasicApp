// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Chat.Operations.ChatCalls;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 通话的穿透参数
/// </summary>
/// <remarks>
/// 来自配置节 <see cref="ConfigurationSection"/>：浏览器之间建立媒体连接要用的 STUN 与 TURN。都不配也能用，只是只有同一网段或能直连的两端打得通
/// TURN 走共享密钥的限时凭据（coturn 的 <c>use-auth-secret</c>）：用户名是「到期秒数:用户」，凭据是它的 HMAC-SHA1，密钥只留在服务端
/// 写错启动即失败：地址的协议不对、配了 TURN 地址没配密钥、时长不是 <c>hh:mm:ss</c> 的正时长
/// </remarks>
public sealed record ChatCallOptions
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Chat:Calls";

    /// <summary>
    /// STUN 地址（<c>stun:</c> 或 <c>stuns:</c> 开头）
    /// </summary>
    public IReadOnlyList<string> StunUrls { get; init; } = [];

    /// <summary>
    /// TURN 地址（<c>turn:</c> 或 <c>turns:</c> 开头）
    /// </summary>
    public IReadOnlyList<string> TurnUrls { get; init; } = [];

    /// <summary>
    /// TURN 的共享密钥
    /// </summary>
    /// <remarks>
    /// 与 TURN 服务器上配的同一个值，不下发给浏览器
    /// </remarks>
    public string TurnSecret { get; init; } = string.Empty;

    /// <summary>
    /// TURN 凭据的有效期
    /// </summary>
    public TimeSpan TurnCredentialLifetime { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 从配置读取
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <returns>参数</returns>
    public static ChatCallOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var defaults = new ChatCallOptions();
        var options = new ChatCallOptions
        {
            StunUrls = Urls(section, nameof(StunUrls), "stun:", "stuns:"),
            TurnUrls = Urls(section, nameof(TurnUrls), "turn:", "turns:"),
            TurnSecret = section[nameof(TurnSecret)]?.Trim() ?? string.Empty,
            TurnCredentialLifetime = Duration(section, nameof(TurnCredentialLifetime), defaults.TurnCredentialLifetime),
        };
        return options.TurnUrls.Count > 0 && options.TurnSecret.Length == 0
            ? throw new ArgumentException($"'{ConfigurationSection}:{nameof(TurnSecret)}' is required when '{ConfigurationSection}:{nameof(TurnUrls)}' is set.")
            : options;
    }

    /// <summary>
    /// 给一个用户的 ICE 服务器
    /// </summary>
    /// <param name="userId">用户</param>
    /// <param name="now">此刻（UTC）</param>
    /// <returns>STUN 一组、TURN 一组（带现签的凭据）与凭据的到期时间</returns>
    public ChatIceServersResponse IceServersFor(Guid userId, DateTimeOffset now)
    {
        var servers = new List<ChatIceServer>(2);
        if (StunUrls.Count > 0)
        {
            servers.Add(new ChatIceServer(StunUrls, null, null));
        }

        if (TurnUrls.Count == 0)
        {
            return new ChatIceServersResponse(servers, null);
        }

        var expiresAt = now + TurnCredentialLifetime;
        var username = string.Create(CultureInfo.InvariantCulture, $"{expiresAt.ToUnixTimeSeconds()}:{userId:N}");
#pragma warning disable CA5350 // TURN 的限时凭据由协议定为 HMAC-SHA1（coturn 的 use-auth-secret），换算法服务器不认
        var credential = Convert.ToBase64String(HMACSHA1.HashData(Encoding.UTF8.GetBytes(TurnSecret), Encoding.UTF8.GetBytes(username)));
#pragma warning restore CA5350
        servers.Add(new ChatIceServer(TurnUrls, username, credential));
        return new ChatIceServersResponse(servers, expiresAt);
    }

    private static List<string> Urls(IConfigurationSection section, string key, params string[] schemes)
    {
        var urls = section.GetSection(key).GetChildren().Select(static child => child.Value?.Trim()).Where(static value => !string.IsNullOrEmpty(value)).Select(static value => value!).ToList();
        foreach (var url in urls)
        {
            if (!schemes.Any(scheme => url.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) && url.Length > scheme.Length))
            {
                throw new ArgumentException($"'{ConfigurationSection}:{key}' entries must start with {string.Join(" or ", schemes.Select(static scheme => $"'{scheme}'"))}, but one was '{url}'.");
            }
        }

        return urls;
    }

    private static TimeSpan Duration(IConfigurationSection section, string key, TimeSpan defaultValue)
    {
        if (section[key] is not { Length: > 0 } raw)
        {
            return defaultValue;
        }

        // 少了冒号的（3600、01:00）TimeSpan 也认，只是读成 3600 天、1 小时 0 分，按写错处理
        return raw.Count(static c => c == ':') == 2 && TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var value) && value > TimeSpan.Zero
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a positive duration written as 'hh:mm:ss', but was '{raw}'.");
    }
}
