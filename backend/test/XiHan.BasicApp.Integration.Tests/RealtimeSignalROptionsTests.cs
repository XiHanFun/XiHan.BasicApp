// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Realtime;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 SignalR 参数的配置节
/// </summary>
/// <remarks>
/// 没写即 4.x 的缺省值，写了的进 Hub 选项，写错启动即失败
/// </remarks>
public sealed class RealtimeSignalROptionsTests : IDisposable
{
    private const string Section = "BasicApp:Realtime:SignalR:";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static RealtimeSignalROptions Read(params (string Key, string Value)[] settings)
        => RealtimeSignalROptions.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(settings.Select(static s => new KeyValuePair<string, string?>(Section + s.Key, s.Value))).Build());

    [Fact]
    public void Defaults_AreThoseOf4x()
    {
        var options = Read();
        Assert.False(options.EnableDetailedErrors);
        Assert.Equal(TimeSpan.FromSeconds(15), options.KeepAliveInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ClientTimeoutInterval);
        Assert.Equal(TimeSpan.FromSeconds(15), options.HandshakeTimeout);
        Assert.Equal(32 * 1024, options.MaximumReceiveMessageSize);
        Assert.Equal(10, options.StreamBufferCapacity);
        Assert.Equal(1, options.MaximumParallelInvocationsPerClient);
    }

    [Fact]
    public void Durations_MayCarryDays()
        => Assert.Equal(TimeSpan.FromDays(1), Read(("KeepAliveInterval", "1.00:00:00")).KeepAliveInterval);

    [Theory]
    [InlineData("EnableDetailedErrors", "yes")]
    [InlineData("KeepAliveInterval", "15")]
    [InlineData("KeepAliveInterval", "00:15")]
    [InlineData("KeepAliveInterval", "soon")]
    [InlineData("ClientTimeoutInterval", "-00:00:30")]
    [InlineData("HandshakeTimeout", "00:00:00")]
    [InlineData("MaximumReceiveMessageSize", "0")]
    [InlineData("MaximumReceiveMessageSize", "32KB")]
    [InlineData("StreamBufferCapacity", "-1")]
    [InlineData("StreamBufferCapacity", "2147483648")]
    [InlineData("MaximumParallelInvocationsPerClient", "0")]
    public void AWrongValue_IsRejected_NamingTheKey(string key, string value)
    {
        var rejected = Assert.Throws<ArgumentException>(() => Read((key, value)));
        Assert.Contains(Section + key, rejected.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfiguredValues_ReachTheHubOptions()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings:
        [
            new(Section + "EnableDetailedErrors", "true"),
            new(Section + "KeepAliveInterval", "00:00:20"),
            new(Section + "ClientTimeoutInterval", "00:01:00"),
            new(Section + "HandshakeTimeout", "00:00:05"),
            new(Section + "MaximumReceiveMessageSize", "65536"),
            new(Section + "StreamBufferCapacity", "20"),
            new(Section + "MaximumParallelInvocationsPerClient", "4"),
        ]);
        var hub = server.Services.GetRequiredService<IOptions<HubOptions>>().Value;
        Assert.True(hub.EnableDetailedErrors);
        Assert.Equal(TimeSpan.FromSeconds(20), hub.KeepAliveInterval);
        Assert.Equal(TimeSpan.FromMinutes(1), hub.ClientTimeoutInterval);
        Assert.Equal(TimeSpan.FromSeconds(5), hub.HandshakeTimeout);
        Assert.Equal(65536, hub.MaximumReceiveMessageSize);
        Assert.Equal(20, hub.StreamBufferCapacity);
        Assert.Equal(4, hub.MaximumParallelInvocationsPerClient);
    }

    [Fact]
    public async Task AWrongValue_FailsTheStartup()
    {
        var failed = await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new(Section + "KeepAliveInterval", "15")]));
        Assert.Contains(Section + "KeepAliveInterval", failed.ToString(), StringComparison.Ordinal);
    }
}
