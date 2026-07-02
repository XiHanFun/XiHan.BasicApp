#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasConfigurationSeeder
// Guid:f78f58bb-a5a4-4813-8b8d-7d7187a4c43c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Bot.Telegram.Options;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 系统参数种子数据
/// </summary>
public sealed class SaasConfigurationSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasConfigurationSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 22;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统参数种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var definitions = BuildDefinitions();
        var configKeys = definitions
            .Select(static definition => definition.ConfigKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var existingConfigs = await client.Queryable<SysConfig>()
            .Where(config => config.TenantId == 0 && configKeys.Contains(config.ConfigKey))
            .ToListAsync();
        var configMap = existingConfigs
            .GroupBy(config => config.ConfigKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var addList = new List<SysConfig>();
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (configMap.TryGetValue(definition.ConfigKey, out var existing))
            {
                if (ApplyDefinition(existing, definition, fillValueOnly: true))
                {
                    _ = await client.Updateable(existing).ExecuteCommandAsync();
                    updateCount++;
                }

                continue;
            }

            addList.Add(CreateConfig(definition));
        }

        if (addList.Count > 0)
        {
            await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        }

        if (addList.Count == 0 && updateCount == 0)
        {
            Logger.LogInformation("SaaS 系统参数数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化 SaaS 系统参数，新增 {AddCount} 个，更新 {UpdateCount} 个", addList.Count, updateCount);
    }

    private static SysConfig CreateConfig(ConfigSeedDefinition definition)
    {
        var config = new SysConfig
        {
            ConfigKey = definition.ConfigKey
        };
        _ = ApplyDefinition(config, definition, fillValueOnly: false);
        return config;
    }

    private static bool ApplyDefinition(SysConfig config, ConfigSeedDefinition definition, bool fillValueOnly)
    {
        var changed = false;
        changed |= SetIfChanged(config.TenantId, 0, value => config.TenantId = value);
        changed |= SetIfChanged(config.ConfigName, definition.ConfigName, value => config.ConfigName = value);
        changed |= SetIfChanged(config.ConfigGroup, definition.ConfigGroup, value => config.ConfigGroup = value);
        changed |= SetIfChanged(config.ConfigKey, definition.ConfigKey, value => config.ConfigKey = value);
        changed |= SetIfChanged(config.DefaultValue, definition.DefaultValue, value => config.DefaultValue = value);
        changed |= SetIfChanged(config.ConfigType, definition.ConfigType, value => config.ConfigType = value);
        changed |= SetIfChanged(config.DataType, definition.DataType, value => config.DataType = value);
        changed |= SetIfChanged(config.ConfigDescription, definition.ConfigDescription, value => config.ConfigDescription = value);
        changed |= SetIfChanged(config.IsBuiltIn, true, value => config.IsBuiltIn = value);
        changed |= SetIfChanged(config.IsEncrypted, definition.IsEncrypted, value => config.IsEncrypted = value);
        changed |= SetIfChanged(config.Status, EnableStatus.Enabled, value => config.Status = value);
        changed |= SetIfChanged(config.Sort, definition.Sort, value => config.Sort = value);
        changed |= SetIfChanged(config.Remark, "系统初始化内置参数", value => config.Remark = value);

        if (!fillValueOnly || string.IsNullOrWhiteSpace(config.ConfigValue))
        {
            changed |= SetIfChanged(config.ConfigValue, definition.ConfigValue, value => config.ConfigValue = value);
        }

        return changed;
    }

    private static IReadOnlyList<ConfigSeedDefinition> BuildDefinitions()
    {
        // 仅保留运行期被强类型读取入口实际消费的配置键：
        // - SaasConfigurationService.GetLoginConfigAsync（认证）
        // - SaasTelegramBotSettingsStore.GetSettingsAsync（Telegram 机器人平台设置）
        // 其余历史占位键未被任何代码引用，已移除以避免无效内置参数（详见配置审计）。
        return
        [
            new("登录方式", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.LoginMethods, "[\"password\"]", "[\"password\"]", ConfigType.Feature, ConfigDataType.Array, "登录页开放的登录方式编码集合", 50),
            new("OAuth 提供商", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.OAuthProviders, "[{\"name\":\"github\",\"displayName\":\"Github\"},{\"name\":\"google\",\"displayName\":\"Google\"},{\"name\":\"qq\",\"displayName\":\"QQ\"}]", "[]", ConfigType.Feature, ConfigDataType.Array, "登录页展示的 OAuth 提供商 JSON 数组", 70),
            new("Telegram 机器人开关", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.Enabled, "false", "false", ConfigType.Feature, ConfigDataType.Boolean, "是否启用 Telegram 机器人平台（总开关，关闭时不拉起任何机器人）", 100),
            new("Telegram Webhook 基础地址", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.WebhookBaseUrl, "", "", ConfigType.Feature, ConfigDataType.String, "Webhook 基础地址（如 https://example.com）；留空使用长轮询（Polling）模式", 101),
            new("Telegram Webhook 路由前缀", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.WebhookRoutePrefix, TelegramBotPlatformConsts.DefaultWebhookRoutePrefix, TelegramBotPlatformConsts.DefaultWebhookRoutePrefix, ConfigType.Feature, ConfigDataType.String, "Webhook 接收中间件路由前缀（匹配 POST {前缀}/{机器人名}）", 102),
            new("Telegram Webhook 密钥令牌", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.WebhookSecretToken, "", "", ConfigType.Feature, ConfigDataType.String, "Webhook 模式必填的 secret_token（未配置一律拒绝 Webhook 请求，fail-closed）", 103, IsEncrypted: true),
            new("Telegram 管理器刷新秒数", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.ManagerRefreshSeconds, "5", "5", ConfigType.Feature, ConfigDataType.Number, "管理器动态增删改机器人的探测周期（秒）", 104),
            new("Telegram 配置缓存秒数", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.ConfigCacheSeconds, "5", "5", ConfigType.Feature, ConfigDataType.Number, "机器人配置列表的进程内缓存时长（秒）", 105),
            new("Telegram 兜底回复", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.EnableFallbackReply, "false", "false", ConfigType.Feature, ConfigDataType.Boolean, "无处理器命中普通消息时是否回复提示文案（与单机器人配置任一开启即生效）", 106),
            new("Telegram 代理地址", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.ProxyUrl, "", "", ConfigType.Feature, ConfigDataType.String, "访问 Telegram API 的代理地址（如 http://127.0.0.1:7890 或 socks5://127.0.0.1:1080）；留空直连", 107),
            new("Telegram 请求超时秒数", SaasConfigKeys.Groups.Bot, SaasConfigKeys.Bot.Telegram.TimeoutSeconds, "100", "100", ConfigType.Feature, ConfigDataType.Number, "Telegram API 请求超时（秒）", 108)
        ];
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    private sealed record ConfigSeedDefinition(
        string ConfigName,
        string ConfigGroup,
        string ConfigKey,
        string ConfigValue,
        string DefaultValue,
        ConfigType ConfigType,
        ConfigDataType DataType,
        string ConfigDescription,
        int Sort,
        bool IsEncrypted = false);
}
