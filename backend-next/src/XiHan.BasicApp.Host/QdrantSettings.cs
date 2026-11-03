// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 向量库连接 <c>BasicApp:AI:Qdrant</c>
/// </summary>
/// <remarks>
/// 配了 <c>Host</c> 才登记 Qdrant 连接器并装框架的知识库 Provider（方案 D15）；没配即知识库不可用、操作返回框架的 <c>AI_UNAVAILABLE_002</c>，不悄悄退到内存
/// 向量维度、检索条数与切块参数在框架节 <c>XiHan:AI:Knowledge</c>
/// </remarks>
public sealed record QdrantSettings
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string ConfigurationSection = "BasicApp:AI:Qdrant";

    /// <summary>
    /// Qdrant 的 gRPC 缺省端口
    /// </summary>
    public const int DefaultPort = 6334;

    /// <summary>
    /// 主机名
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// gRPC 端口
    /// </summary>
    public int Port { get; init; } = DefaultPort;

    /// <summary>
    /// 是否走 HTTPS
    /// </summary>
    public bool Https { get; init; }

    /// <summary>
    /// API 密钥
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// 从配置读取
    /// </summary>
    /// <remarks>
    /// <c>Host</c> 没写或是空白即没配；端口与 HTTPS 没写取缺省，写错启动即报错
    /// </remarks>
    /// <returns>配了的连接，没配是 <c>null</c></returns>
    public static QdrantSettings? FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var host = configuration[$"{ConfigurationSection}:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var apiKey = configuration[$"{ConfigurationSection}:ApiKey"];
        return new QdrantSettings
        {
            Host = host.Trim(),
            Port = ReadPort(configuration),
            Https = ReadHttps(configuration),
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
        };
    }

    private static int ReadPort(IConfiguration configuration)
    {
        var raw = configuration[$"{ConfigurationSection}:Port"];
        if (string.IsNullOrEmpty(raw))
        {
            return DefaultPort;
        }

        return int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is >= 1 and <= 65535
            ? port
            : throw new ArgumentException($"'{ConfigurationSection}:Port' must be a port number between 1 and 65535, but was '{raw}'.");
    }

    private static bool ReadHttps(IConfiguration configuration)
    {
        var raw = configuration[$"{ConfigurationSection}:Https"];
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        return bool.TryParse(raw, out var https)
            ? https
            : throw new ArgumentException($"'{ConfigurationSection}:Https' must be 'true' or 'false', but was '{raw}'.");
    }
}
