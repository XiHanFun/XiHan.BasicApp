// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// SignalR 的 Hub 参数（配置节 <c>BasicApp:Realtime:SignalR</c>）
/// </summary>
/// <remarks>
/// 键、语义与缺省照 4.x 的 <c>XiHan:Web:RealTime:SignalR</c>，缺省值也就是 ASP.NET Core 的缺省值。对全部 Hub 生效。4.x 还有一个 <c>EnableConnectionMetrics</c>，没有任何消费方，没有搬
/// 时长写 <c>hh:mm:ss</c>（可带天数 <c>d.hh:mm:ss</c>）
/// </remarks>
public sealed record RealtimeSignalROptions
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Realtime:SignalR";

    /// <summary>
    /// 是否把详细错误发给客户端
    /// </summary>
    /// <remarks>
    /// 配置键 <c>EnableDetailedErrors</c>，缺省关，生产环境不要打开
    /// </remarks>
    public bool EnableDetailedErrors { get; init; }

    /// <summary>
    /// 服务端发保活消息的间隔
    /// </summary>
    /// <remarks>
    /// 配置键 <c>KeepAliveInterval</c>，缺省 15 秒
    /// </remarks>
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 多久没收到客户端的消息算断开
    /// </summary>
    /// <remarks>
    /// 配置键 <c>ClientTimeoutInterval</c>，缺省 30 秒
    /// </remarks>
    public TimeSpan ClientTimeoutInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 握手的超时
    /// </summary>
    /// <remarks>
    /// 配置键 <c>HandshakeTimeout</c>，缺省 15 秒
    /// </remarks>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 单条入站消息的大小上限（字节）
    /// </summary>
    /// <remarks>
    /// 配置键 <c>MaximumReceiveMessageSize</c>，缺省 32 KB
    /// </remarks>
    public long MaximumReceiveMessageSize { get; init; } = 32 * 1024;

    /// <summary>
    /// 流式调用的缓冲容量
    /// </summary>
    /// <remarks>
    /// 配置键 <c>StreamBufferCapacity</c>，缺省 10
    /// </remarks>
    public int StreamBufferCapacity { get; init; } = 10;

    /// <summary>
    /// 每个客户端的最大并行调用数
    /// </summary>
    /// <remarks>
    /// 配置键 <c>MaximumParallelInvocationsPerClient</c>，缺省 1
    /// </remarks>
    public int MaximumParallelInvocationsPerClient { get; init; } = 1;

    /// <summary>
    /// 读取配置
    /// </summary>
    /// <exception cref="ArgumentException">配置写错，启动即失败</exception>
    public static RealtimeSignalROptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var defaults = new RealtimeSignalROptions();
        return new RealtimeSignalROptions
        {
            EnableDetailedErrors = Switch(section, nameof(EnableDetailedErrors), defaults.EnableDetailedErrors),
            KeepAliveInterval = Duration(section, nameof(KeepAliveInterval), defaults.KeepAliveInterval),
            ClientTimeoutInterval = Duration(section, nameof(ClientTimeoutInterval), defaults.ClientTimeoutInterval),
            HandshakeTimeout = Duration(section, nameof(HandshakeTimeout), defaults.HandshakeTimeout),
            MaximumReceiveMessageSize = Size(section, nameof(MaximumReceiveMessageSize), defaults.MaximumReceiveMessageSize),
            StreamBufferCapacity = (int)Size(section, nameof(StreamBufferCapacity), defaults.StreamBufferCapacity, int.MaxValue),
            MaximumParallelInvocationsPerClient = (int)Size(section, nameof(MaximumParallelInvocationsPerClient), defaults.MaximumParallelInvocationsPerClient, int.MaxValue),
        };
    }

    /// <summary>
    /// 写进 SignalR 的 Hub 选项
    /// </summary>
    /// <param name="hub">Hub 选项</param>
    public void Apply(HubOptions hub)
    {
        ArgumentNullException.ThrowIfNull(hub);
        hub.EnableDetailedErrors = EnableDetailedErrors;
        hub.KeepAliveInterval = KeepAliveInterval;
        hub.ClientTimeoutInterval = ClientTimeoutInterval;
        hub.HandshakeTimeout = HandshakeTimeout;
        hub.MaximumReceiveMessageSize = MaximumReceiveMessageSize;
        hub.StreamBufferCapacity = StreamBufferCapacity;
        hub.MaximumParallelInvocationsPerClient = MaximumParallelInvocationsPerClient;
    }

    private static bool Switch(IConfigurationSection section, string key, bool defaultValue)
    {
        if (section[key] is not { Length: > 0 } raw)
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var value)
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be 'true' or 'false', but was '{raw}'.");
    }

    private static TimeSpan Duration(IConfigurationSection section, string key, TimeSpan defaultValue)
    {
        if (section[key] is not { Length: > 0 } raw)
        {
            return defaultValue;
        }

        // 少了冒号的（15、00:15）TimeSpan 也认，只是读成 15 天、15 分钟，按写错处理
        return raw.Count(static c => c == ':') == 2 && TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var value) && value > TimeSpan.Zero
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a positive duration written as 'hh:mm:ss', but was '{raw}'.");
    }

    private static long Size(IConfigurationSection section, string key, long defaultValue, long maximum = long.MaxValue)
    {
        if (section[key] is not { Length: > 0 } raw)
        {
            return defaultValue;
        }

        return long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0 && value <= maximum
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a positive integer no larger than {maximum}, but was '{raw}'.");
    }
}
