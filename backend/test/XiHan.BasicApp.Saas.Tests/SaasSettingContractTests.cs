// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 参数配置契约测试：JSON 参数的读取口径、登录设置的校验，以及参数种子声明与设置类型、升级脚本的一致性。
/// </summary>
/// <remarks>
/// 同一功能的设置合成一条 JSON 参数，结构只在设置类型里定义一处；种子的初始值与默认值由设置类型序列化得到，
/// 升级脚本合并旧参数时写入的默认值也必须与之一致，否则参数页里看到的默认值与运行时用的不是一回事。
/// </remarks>
public sealed class SaasSettingContractTests
{
    private static readonly Dictionary<string, Type> JsonSettingTypes = new(StringComparer.Ordinal)
    {
        [SaasConfigKeys.Auth.Login] = typeof(SaasLoginSettings),
        [SaasConfigKeys.Auth.Password] = typeof(SaasPasswordSettings),
        [SaasConfigKeys.Auth.Impersonation] = typeof(SaasImpersonationSettings),
        [SaasConfigKeys.Bot.Telegram.Settings] = typeof(SaasTelegramBotConfig),
    };

    /// <summary>
    /// 未配置或值为空白时按调用方给的默认值运行。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetJsonAsync_MissingOrBlank_ShouldReturnDefault(string? value)
    {
        var service = CreateService(SaasConfigKeys.Auth.Impersonation, value);
        var fallback = new SaasImpersonationSettings { SessionMinutes = 15 };

        var settings = await service.GetJsonAsync(SaasConfigKeys.Auth.Impersonation, fallback);

        Assert.Same(fallback, settings);
    }

    /// <summary>
    /// 只写了部分字段时，没写的字段取设置类型的默认值。
    /// </summary>
    [Fact]
    public async Task GetJsonAsync_PartialObject_ShouldKeepTypeDefaultsForMissingFields()
    {
        var service = CreateService(SaasConfigKeys.Auth.Impersonation, """{"sessionMinutes":10}""");

        var settings = await service.GetJsonAsync(SaasConfigKeys.Auth.Impersonation, new SaasImpersonationSettings());

        Assert.Equal(10, settings.SessionMinutes);
        Assert.True(settings.NotifyTarget);
    }

    /// <summary>
    /// 值写错（不是 JSON、类型不对、数字写成字符串、字面量 null）直接报错并指明参数键，不静默回退默认值。
    /// </summary>
    [Theory]
    [InlineData("30")]
    [InlineData("{sessionMinutes:10}")]
    [InlineData("""{"sessionMinutes":"10"}""")]
    [InlineData("""{"notifyTarget":"true"}""")]
    [InlineData("null")]
    public async Task GetJsonAsync_Malformed_ShouldThrowNamingTheKey(string value)
    {
        var service = CreateService(SaasConfigKeys.Auth.Impersonation, value);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetJsonAsync(SaasConfigKeys.Auth.Impersonation, new SaasImpersonationSettings()));

        Assert.Contains(SaasConfigKeys.Auth.Impersonation, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 登录设置：登录方式去空白、去重；第三方登录没写展示名时用方案名，没写方案名的忽略。
    /// </summary>
    [Fact]
    public async Task GetLoginConfigAsync_ShouldNormalizeMethodsAndProviders()
    {
        var service = CreateService(
            SaasConfigKeys.Auth.Login,
            """{"methods":[" password ","PASSWORD","email",""],"oauthProviders":[{"name":"github"},{"name":"wechat","displayName":"微信"},{"name":" "}]}""");

        var config = await service.GetLoginConfigAsync();

        Assert.Equal(["password", "email"], config.LoginMethods);
        Assert.Equal(["github", "wechat"], config.OAuthProviders.Select(static provider => provider.Name));
        Assert.Equal(["github", "微信"], config.OAuthProviders.Select(static provider => provider.DisplayName));
    }

    /// <summary>
    /// 登录设置未配置时只开放密码登录、不展示第三方登录。
    /// </summary>
    [Fact]
    public async Task GetLoginConfigAsync_Missing_ShouldOfferPasswordOnly()
    {
        var service = CreateService(SaasConfigKeys.Auth.Login, null);

        var config = await service.GetLoginConfigAsync();

        Assert.Equal(["password"], config.LoginMethods);
        Assert.Empty(config.OAuthProviders);
    }

    /// <summary>
    /// 登录方式全被关掉会让任何人都登不进来：直接报错，不偷偷补回密码登录。
    /// </summary>
    [Theory]
    [InlineData("""{"methods":[]}""")]
    [InlineData("""{"methods":["", " "]}""")]
    [InlineData("""{"methods":null}""")]
    public async Task GetLoginConfigAsync_NoMethod_ShouldThrow(string value)
    {
        var service = CreateService(SaasConfigKeys.Auth.Login, value);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetLoginConfigAsync());

        Assert.Contains(SaasConfigKeys.Auth.Login, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 参数种子：键唯一且合法，都是平台内置参数的正确类型；JSON 参数的初始值与默认值是设置类型的规范序列化，
    /// 默认值就是设置类型本身的默认实例，中文不转义。
    /// </summary>
    [Fact]
    public void SaasSettingSeeder_DeclarationsShouldMatchSettingTypes()
    {
        var settings = CreateSeeder().Settings;

        Assert.Equal(settings.Count, settings.Select(static setting => setting.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(settings.Count, settings.Select(static setting => setting.Sort).Distinct().Count());
        foreach (var setting in settings)
        {
            Assert.Equal(setting.Key, SaasConfigKeys.Normalize(setting.Key), StringComparer.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(setting.Name));
            Assert.False(string.IsNullOrWhiteSpace(setting.Description));
        }

        var jsonSettings = settings.Where(static setting => setting.DataType == ConfigDataType.Json).ToList();
        Assert.Equal(JsonSettingTypes.Keys.Order(StringComparer.Ordinal), jsonSettings.Select(static setting => setting.Key).Order(StringComparer.Ordinal));
        foreach (var setting in jsonSettings)
        {
            var type = JsonSettingTypes[setting.Key];
            Assert.Equal(Canonical(setting.Value, type), setting.Value, StringComparer.Ordinal);
            Assert.Equal(JsonSerializer.Serialize(Activator.CreateInstance(type), type, SaasConfigurationService.JsonOptions), setting.DefaultValue, StringComparer.Ordinal);
        }

        var login = settings.Single(static setting => setting.Key == SaasConfigKeys.Auth.Login);
        Assert.Contains("\"企业微信\"", login.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// 强制改密默认关闭：参数的初始值与默认值都是关闭。
    /// </summary>
    [Fact]
    public void SaasSettingSeeder_ForceChangeShouldBeSeededOff()
    {
        var password = CreateSeeder().Settings.Single(static setting => setting.Key == SaasConfigKeys.Auth.Password);

        Assert.False(JsonSerializer.Deserialize<SaasPasswordSettings>(password.Value, SaasConfigurationService.JsonOptions)!.ForceChange);
        Assert.False(JsonSerializer.Deserialize<SaasPasswordSettings>(password.DefaultValue, SaasConfigurationService.JsonOptions)!.ForceChange);
    }

    /// <summary>
    /// 密钥类参数加密存储、初始为空；其余参数不加密。
    /// </summary>
    [Fact]
    public void SaasSettingSeeder_OnlySecretsShouldBeEncrypted()
    {
        var settings = CreateSeeder().Settings;

        var encrypted = Assert.Single(settings, static setting => setting.IsEncrypted);
        Assert.Equal(SaasConfigKeys.Bot.Telegram.WebhookSecretToken, encrypted.Key, StringComparer.Ordinal);
        Assert.Equal(string.Empty, encrypted.Value);
    }

    /// <summary>
    /// 日志保留天数的种子值与清理任务的缺省天数一致。
    /// </summary>
    [Fact]
    public void SaasSettingSeeder_LogRetentionShouldMatchTaskDefault()
    {
        var retention = CreateSeeder().Settings.Single(static setting => setting.Key == SaasConfigKeys.Log.RetentionDays);

        Assert.Equal(ConfigDataType.Number, retention.DataType);
        Assert.Equal(LogRetentionCleanupTask.DefaultRetentionDays.ToString(System.Globalization.CultureInfo.InvariantCulture), retention.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// 5.3.0 升级脚本把旧参数合并成新键时写入的默认值，必须与种子声明的默认值一字不差。
    /// </summary>
    [Fact]
    public void UpgradeScript_MergedDefaultsShouldMatchSeeds()
    {
        var script = File.ReadAllText(ResolveUpgradeScript("5.3.0"));

        foreach (var key in new[] { SaasConfigKeys.Auth.Login, SaasConfigKeys.Auth.Impersonation, SaasConfigKeys.Bot.Telegram.Settings, SaasConfigKeys.Log.RetentionDays })
        {
            var setting = CreateSeeder().Settings.Single(setting => setting.Key == key);
            Assert.Contains($"'{key}'", script, StringComparison.Ordinal);
            Assert.Contains($"'{setting.DefaultValue}'", script, StringComparison.Ordinal);
        }
    }

    private static string Canonical(string json, Type type)
    {
        return JsonSerializer.Serialize(JsonSerializer.Deserialize(json, type, SaasConfigurationService.JsonOptions), type, SaasConfigurationService.JsonOptions);
    }

    private static SaasSettingSeeder CreateSeeder()
    {
        return new SaasSettingSeeder(Mock.Of<ISqlSugarClientResolver>(), NullLogger<SaasSettingSeeder>.Instance, Mock.Of<IServiceProvider>());
    }

    private static SaasConfigurationService CreateService(string configKey, string? value)
    {
        var query = new Mock<ISaasConfigValueQueryService>();
        query
            .Setup(service => service.GetValueItemAsync(configKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaasConfigValueCacheItem { ConfigKey = configKey, Value = value, Exists = value is not null });
        return new SaasConfigurationService(query.Object);
    }

    private static string ResolveUpgradeScript(string version, [CallerFilePath] string testFilePath = "")
    {
        var testDirectory = Path.GetDirectoryName(testFilePath)
            ?? throw new InvalidOperationException("无法解析测试源文件目录。");

        return Path.GetFullPath(Path.Combine(
            testDirectory, "..", "..", "src", "main", "XiHan.BasicApp.WebHost", "UpdateScripts", version, $"{version}.sql"));
    }
}
