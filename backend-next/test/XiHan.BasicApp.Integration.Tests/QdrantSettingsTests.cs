// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Host;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证向量库配置 <c>BasicApp:AI:Qdrant</c> 的读取
/// </summary>
public sealed class QdrantSettingsTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(static v => new KeyValuePair<string, string?>($"{QdrantSettings.ConfigurationSection}:{v.Key}", v.Value)))
            .Build();

    [Fact]
    public void WithoutAHost_ThereIsNoVectorStore()
    {
        Assert.Null(QdrantSettings.FromConfiguration(Configuration()));
        Assert.Null(QdrantSettings.FromConfiguration(Configuration(("Host", "   "), ("Port", "1234"))));
    }

    [Fact]
    public void Defaults_AreTheGrpcPortOverPlainHttp()
    {
        var settings = QdrantSettings.FromConfiguration(Configuration(("Host", " qdrant.local ")));
        Assert.NotNull(settings);
        Assert.Equal("qdrant.local", settings.Host);
        Assert.Equal(6334, settings.Port);
        Assert.False(settings.Https);
        Assert.Null(settings.ApiKey);
    }

    [Fact]
    public void ExplicitValues_AreRead()
    {
        var settings = QdrantSettings.FromConfiguration(Configuration(("Host", "qdrant.local"), ("Port", "16334"), ("Https", "true"), ("ApiKey", "placeholder")));
        Assert.NotNull(settings);
        Assert.Equal(16334, settings.Port);
        Assert.True(settings.Https);
        Assert.Equal("placeholder", settings.ApiKey);
    }

    [Theory]
    [InlineData("Port", "not-a-port")]
    [InlineData("Port", "0")]
    [InlineData("Port", "65536")]
    [InlineData("Https", "maybe")]
    public void WrongValues_FailAtStartup(string key, string value)
    {
        var failure = Assert.Throws<ArgumentException>(() => QdrantSettings.FromConfiguration(Configuration(("Host", "qdrant.local"), (key, value))));
        Assert.Contains($"{QdrantSettings.ConfigurationSection}:{key}", failure.Message, StringComparison.Ordinal);
    }
}
