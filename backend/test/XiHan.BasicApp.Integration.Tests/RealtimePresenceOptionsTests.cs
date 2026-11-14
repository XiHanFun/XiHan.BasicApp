// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Realtime;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证在线状态发布参数的配置节
/// </summary>
/// <remarks>
/// 没写即缺省值，写错启动即失败；宽限真正起的作用由 <see cref="ChatTests"/> 的在线状态用例验证
/// </remarks>
public sealed class RealtimePresenceOptionsTests : IDisposable
{
    private const string Key = "BasicApp:Realtime:Presence:OfflineGrace";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static RealtimePresenceOptions Read(string? value)
        => RealtimePresenceOptions.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection([new KeyValuePair<string, string?>(Key, value)]).Build());

    [Fact]
    public void OfflineGrace_DefaultsToEightSeconds_AndReadsWhatIsWritten()
    {
        Assert.Equal(TimeSpan.FromSeconds(8), Read(null).OfflineGrace);
        Assert.Equal(TimeSpan.FromSeconds(30), Read("00:00:30").OfflineGrace);
    }

    [Theory]
    [InlineData("8")]
    [InlineData("00:08")]
    [InlineData("00:00:00")]
    [InlineData("-00:00:08")]
    public void AWrongValue_IsRejected_NamingTheKey(string value)
    {
        var rejected = Assert.Throws<ArgumentException>(() => Read(value));
        Assert.Contains(Key, rejected.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AWrongValue_FailsTheStartup()
    {
        var failed = await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new(Key, "8")]));
        Assert.Contains(Key, failed.ToString(), StringComparison.Ordinal);
    }
}
