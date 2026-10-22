// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Security;
using XiHan.BasicApp.Saas.Infrastructure.Security;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// IP 黑名单条目的解析与匹配：单个地址、CIDR、IPv6、IPv4 映射地址，以及拒收的写法。
/// </summary>
public sealed class SaasIpBlacklistTests
{
    /// <summary>
    /// 命中与未命中：单个地址只命中它自己，网段按前缀命中，IPv6 同理。
    /// </summary>
    [Theory]
    [InlineData("198.51.100.7", "198.51.100.7", true)]
    [InlineData("198.51.100.7", "198.51.100.8", false)]
    [InlineData("203.0.113.0/24", "203.0.113.0", true)]
    [InlineData("203.0.113.0/24", "203.0.113.255", true)]
    [InlineData("203.0.113.0/24", "203.0.114.0", false)]
    [InlineData("10.0.0.0/8", "10.255.1.2", true)]
    [InlineData("0.0.0.0/0", "8.8.8.8", true)]
    [InlineData("2001:db8::/32", "2001:db8:1234::1", true)]
    [InlineData("2001:db8::/32", "2001:db9::1", false)]
    [InlineData("2001:db8::1", "2001:db8::1", true)]
    [InlineData("203.0.113.0/24", "::ffff:203.0.113.9", true)]
    [InlineData("::ffff:198.51.100.7", "198.51.100.7", true)]
    public void Contains_ShouldMatchAddressesAndNetworks(string entry, string client, bool expected)
    {
        var blacklist = SaasIpBlacklist.Parse([entry]);

        Assert.Empty(blacklist.InvalidEntries);
        Assert.Equal(expected, blacklist.Contains(IPAddress.Parse(client)));
    }

    /// <summary>
    /// 简写的 IPv4、主机位不为零的网段、越界前缀、带区域标识的 IPv6 都不收，记进解析不了的条目。
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("203.0.113")]
    [InlineData("10.1")]
    [InlineData("203.0.113.5/24")]
    [InlineData("203.0.113.0/33")]
    [InlineData("2001:db8::/129")]
    [InlineData("fe80::1%eth0")]
    [InlineData("203.0.113.0/")]
    [InlineData("203.0.113.0/-1")]
    public void Parse_ShouldRejectAmbiguousOrInvalidEntries(string entry)
    {
        var blacklist = SaasIpBlacklist.Parse([entry, "198.51.100.1"]);

        Assert.Equal([entry], blacklist.InvalidEntries);
        Assert.Equal(1, blacklist.Count);
    }

    /// <summary>
    /// 参数值为空白时是空黑名单，不是合法 JSON 时报错，空白条目跳过。
    /// </summary>
    [Fact]
    public void FromJson_ShouldReadEntriesAndRejectBrokenJson()
    {
        Assert.Equal(0, SaasIpBlacklist.FromJson("  ").Count);
        Assert.Equal(2, SaasIpBlacklist.FromJson("""{"entries":["203.0.113.0/24"," ","2001:db8::/32"]}""").Count);
        Assert.Throws<InvalidOperationException>(() => SaasIpBlacklist.FromJson("{entries:"));
        Assert.False(SaasIpBlacklist.Empty.Contains(IPAddress.Loopback));
    }
}

/// <summary>
/// IP 黑名单中间件：命中即 403、未命中放行；经代理时只信任已登记代理给出的转发头；只读平台参数；读不到参数时放行。
/// </summary>
public sealed class SaasIpBlacklistMiddlewareTests
{
    private const string Blacklist = """{"entries":["203.0.113.0/24","2001:db8::/32"]}""";

    /// <summary>
    /// 直连的客户端地址命中黑名单：403，不往后走，响应体是统一的失败响应。
    /// </summary>
    [Fact]
    public async Task Invoke_DirectClientInBlacklist_ShouldReturn403()
    {
        var harness = new Harness(Blacklist);
        var context = harness.CreateContext("203.0.113.7");

        await harness.Middleware.InvokeAsync(context);

        Assert.False(harness.NextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        using var body = JsonDocument.Parse(ReadBody(context));
        Assert.Equal(403, body.RootElement.GetProperty("code").GetInt32());
        // 统一失败响应：message 是通用文案，详情在 data（与开放接口安全中间件同一写法）
        Assert.Equal(SaasIpBlacklistMiddleware.ForbiddenMessage, body.RootElement.GetProperty("data").GetString());
    }

    /// <summary>
    /// 未命中的地址（含 IPv6）放行。
    /// </summary>
    [Theory]
    [InlineData("198.51.100.7")]
    [InlineData("2001:db9::1")]
    public async Task Invoke_ClientNotInBlacklist_ShouldPassThrough(string clientIp)
    {
        var harness = new Harness(Blacklist);
        var context = harness.CreateContext(clientIp);

        await harness.Middleware.InvokeAsync(context);

        Assert.True(harness.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    /// <summary>
    /// 经本机代理（缺省信任回环地址）转发：按 X-Forwarded-For 里代理追加的最后一跳判断，客户端自己伪造的前几项不算。
    /// </summary>
    [Theory]
    [InlineData("203.0.113.9", true)]
    [InlineData("198.51.100.1, 203.0.113.9", true)]
    [InlineData("203.0.113.9, 198.51.100.1", false)]
    public async Task Invoke_BehindTrustedProxy_ShouldUseForwardedClient(string forwardedFor, bool blocked)
    {
        var harness = new Harness(Blacklist);
        var context = harness.CreateContext("127.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        await harness.Middleware.InvokeAsync(context);

        Assert.Equal(!blocked, harness.NextCalled);
    }

    /// <summary>
    /// 连接地址不是已登记的代理时，转发头不可信：不能借伪造的转发头躲过黑名单，也不会被伪造的转发头误封。
    /// </summary>
    [Theory]
    [InlineData("203.0.113.9", "198.51.100.1", true)]
    [InlineData("198.51.100.20", "203.0.113.9", false)]
    public async Task Invoke_FromUntrustedPeer_ShouldIgnoreForwardedHeader(string remoteIp, string forwardedFor, bool blocked)
    {
        var harness = new Harness(Blacklist);
        var context = harness.CreateContext(remoteIp);
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        await harness.Middleware.InvokeAsync(context);

        Assert.Equal(!blocked, harness.NextCalled);
    }

    /// <summary>
    /// 登记了代理网段（与框架的转发头配置同一份）后，从该网段来的转发头可信。
    /// </summary>
    [Fact]
    public async Task Invoke_BehindConfiguredProxyNetwork_ShouldUseForwardedClient()
    {
        var harness = new Harness(Blacklist, options => options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.18.0.0/16")));
        var context = harness.CreateContext("172.18.0.5");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.9";

        await harness.Middleware.InvokeAsync(context);

        Assert.False(harness.NextCalled);
    }

    /// <summary>
    /// 判定不改动请求：放行后连接地址与转发头原样交给后面框架自己的转发头中间件。
    /// </summary>
    [Fact]
    public async Task Invoke_PassThrough_ShouldNotRewriteRequest()
    {
        var harness = new Harness(Blacklist);
        var context = harness.CreateContext("127.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.1";

        await harness.Middleware.InvokeAsync(context);

        Assert.True(harness.NextCalled);
        Assert.Equal(IPAddress.Loopback, context.Connection.RemoteIpAddress);
        Assert.Equal("198.51.100.1", context.Request.Headers["X-Forwarded-For"].ToString());
    }

    /// <summary>
    /// 只读平台那一条：即使请求已在某个租户上下文里，读参数时切到平台。
    /// </summary>
    [Fact]
    public async Task Invoke_ShouldReadPlatformSetting()
    {
        var harness = new Harness(Blacklist, currentTenantId: 42);
        var context = harness.CreateContext("198.51.100.7");

        await harness.Middleware.InvokeAsync(context);

        Assert.Equal([(long?)null], harness.TenantIdsWhileReading);
        Assert.Equal(42, harness.CurrentTenant.Id);
    }

    /// <summary>
    /// 参数读取失败或值无效时放行，不因黑名单自身故障拒绝全部请求。
    /// </summary>
    [Fact]
    public async Task Invoke_SettingUnavailableOrBroken_ShouldPassThrough()
    {
        var failing = new Harness(null, readFails: true);
        var broken = new Harness("{entries:");

        var first = failing.CreateContext("203.0.113.7");
        var second = broken.CreateContext("203.0.113.7");
        await failing.Middleware.InvokeAsync(first);
        await broken.Middleware.InvokeAsync(second);

        Assert.True(failing.NextCalled);
        Assert.True(broken.NextCalled);
    }

    /// <summary>
    /// 参数改了以后下一个请求就按新值判断（参数页保存时清掉参数缓存，这里只核中间件不留旧值）。
    /// </summary>
    [Fact]
    public async Task Invoke_SettingChanged_ShouldUseNewValueOnNextRequest()
    {
        var harness = new Harness(Blacklist);
        await harness.Middleware.InvokeAsync(harness.CreateContext("198.51.100.7"));
        Assert.True(harness.NextCalled);

        harness.Value = """{"entries":["198.51.100.0/24"]}""";
        harness.NextCalled = false;
        await harness.Middleware.InvokeAsync(harness.CreateContext("198.51.100.7"));

        Assert.False(harness.NextCalled);
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class Harness
    {
        private readonly IServiceProvider _services;

        public Harness(string? value, Action<ForwardedHeadersOptions>? configureProxies = null, long? currentTenantId = null, bool readFails = false)
        {
            Value = value;
            CurrentTenant = new TestCurrentTenant(currentTenantId);
            var configuration = new Mock<ISaasConfigurationService>();
            var setup = configuration.Setup(service => service.GetStringAsync(SaasConfigKeys.Security.IpBlacklist, null, It.IsAny<CancellationToken>()));
            if (readFails)
            {
                _ = setup.ThrowsAsync(new InvalidOperationException("缓存与数据库都不可用"));
            }
            else
            {
                _ = setup.ReturnsAsync(() =>
                {
                    TenantIdsWhileReading.Add(CurrentTenant.Id);
                    return Value;
                });
            }

            _services = new ServiceCollection()
                .AddSingleton<ICurrentTenant>(CurrentTenant)
                .AddSingleton(configuration.Object)
                .BuildServiceProvider();

            // 与框架 XiHanWebApiModule 的转发头配置一致：还原 For / Proto / Host，缺省只信任回环地址
            var forwarded = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            };
            configureProxies?.Invoke(forwarded);
            Middleware = new SaasIpBlacklistMiddleware(
                _ =>
                {
                    NextCalled = true;
                    return Task.CompletedTask;
                },
                NullLoggerFactory.Instance,
                Options.Create(forwarded));
        }

        public SaasIpBlacklistMiddleware Middleware { get; }

        public TestCurrentTenant CurrentTenant { get; }

        public List<long?> TenantIdsWhileReading { get; } = [];

        public string? Value { get; set; }

        public bool NextCalled { get; set; }

        public DefaultHttpContext CreateContext(string remoteIp)
        {
            var context = new DefaultHttpContext { RequestServices = _services };
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}

/// <summary>
/// 参数页保存 IP 黑名单时的校验：只认平台那一条，条目都要能解析。
/// </summary>
public sealed class SaasIpBlacklistConfigValidationTests
{
    /// <summary>
    /// 租户不能建自己的 IP 黑名单。
    /// </summary>
    [Fact]
    public async Task CreateConfig_InTenantScope_ShouldBeRejected()
    {
        var fixture = new Fixture(contextTenantId: 7);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConfigAsync(Fixture.Create("""{"entries":["203.0.113.0/24"]}""")));

        Assert.Contains("只在平台生效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 有解析不了的条目时拒绝保存，并点名这些条目。
    /// </summary>
    [Fact]
    public async Task UpdateConfig_WithInvalidEntries_ShouldNameThem()
    {
        var fixture = new Fixture(contextTenantId: null);
        fixture.AddPlatformRow(1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateConfigAsync(Fixture.Update(1, """{"entries":["203.0.113.5/24","10.1","198.51.100.1"]}""")));

        Assert.Contains("203.0.113.5/24", exception.Message, StringComparison.Ordinal);
        Assert.Contains("10.1", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("198.51.100.1", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 合法的值（含空列表）照常保存。
    /// </summary>
    [Theory]
    [InlineData("""{"entries":["203.0.113.0/24","2001:db8::/32","198.51.100.7"]}""")]
    [InlineData("""{"entries":[]}""")]
    public async Task UpdateConfig_WithValidValue_ShouldSave(string value)
    {
        var fixture = new Fixture(contextTenantId: null);
        fixture.AddPlatformRow(1);

        _ = await fixture.Service.UpdateConfigAsync(Fixture.Update(1, value));

        fixture.Configs.Verify(repo => repo.UpdateAsync(It.Is<SysConfig>(row => row.ConfigValue == value), It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class Fixture
    {
        private readonly List<SysConfig> _rows = [];

        public Fixture(long? contextTenantId)
        {
            _ = Configs
                .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysConfig, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysConfig, bool>> predicate, CancellationToken _) => _rows.Any(predicate.Compile()));
            _ = Configs
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _rows.Find(row => row.BasicId == id));
            _ = Configs
                .Setup(repo => repo.AddAsync(It.IsAny<SysConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysConfig row, CancellationToken _) => row);
            _ = Configs
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysConfig row, CancellationToken _) => row);

            Service = new ConfigDomainService(Configs.Object, new Mock<IConfigValueSecretProtector>().Object, new TestCurrentTenant(contextTenantId));
        }

        public ConfigDomainService Service { get; }

        public Mock<IConfigRepository> Configs { get; } = new();

        public static ConfigCreateCommand Create(string value) =>
            new("IP 黑名单", SaasConfigKeys.Groups.Security, SaasConfigKeys.Security.IpBlacklist, value, null, ConfigType.Feature, ConfigDataType.Json, null, false, EnableStatus.Enabled, 0, null);

        public static ConfigUpdateCommand Update(long id, string value) =>
            new(id, "IP 黑名单", SaasConfigKeys.Groups.Security, value, null, ConfigType.Feature, ConfigDataType.Json, null, false, 0, null);

        public void AddPlatformRow(long id)
        {
            var row = new SysConfig { TenantId = 0, ConfigKey = SaasConfigKeys.Security.IpBlacklist, ConfigName = "IP 黑名单" };
            SaasTestHelper.SetBasicId(row, id);
            _rows.Add(row);
        }
    }
}
