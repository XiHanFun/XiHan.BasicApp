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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

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
        var configKeys = definitions.Select(definition => definition.ConfigKey).ToArray();
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
        changed |= SetIfChanged(config.IsGlobal, true, value => config.IsGlobal = value);
        changed |= SetIfChanged(config.ConfigName, definition.ConfigName, value => config.ConfigName = value);
        changed |= SetIfChanged(config.ConfigGroup, definition.ConfigGroup, value => config.ConfigGroup = value);
        changed |= SetIfChanged(config.ConfigKey, definition.ConfigKey, value => config.ConfigKey = value);
        changed |= SetIfChanged(config.DefaultValue, definition.DefaultValue, value => config.DefaultValue = value);
        changed |= SetIfChanged(config.ConfigType, definition.ConfigType, value => config.ConfigType = value);
        changed |= SetIfChanged(config.DataType, definition.DataType, value => config.DataType = value);
        changed |= SetIfChanged(config.ConfigDescription, definition.ConfigDescription, value => config.ConfigDescription = value);
        changed |= SetIfChanged(config.IsBuiltIn, true, value => config.IsBuiltIn = value);
        changed |= SetIfChanged(config.IsEncrypted, false, value => config.IsEncrypted = value);
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
        return
        [
            new("应用名称", "application", "app.name", "XiHan BasicApp", "XiHan BasicApp", ConfigType.Application, ConfigDataType.String, "前端标题、通知和系统标识使用的应用名称", 10),
            new("默认语言", "application", "app.default_language", "zh-CN", "zh-CN", ConfigType.Application, ConfigDataType.String, "系统默认语言区域", 20),
            new("默认租户版本", "tenant", "tenant.default_edition_code", "free", "free", ConfigType.Business, ConfigDataType.String, "新租户未显式选择版本时使用的版本编码", 30),
            new("登录通知开关", "notification", "notification.auth_login_enabled", "true", "true", ConfigType.System, ConfigDataType.Boolean, "登录成功后是否写入并推送当前用户通知", 40),
            new("登出通知开关", "notification", "notification.auth_logout_enabled", "true", "true", ConfigType.System, ConfigDataType.Boolean, "主动退出后是否写入并推送当前用户通知", 50),
            new("密码最小长度", "auth", "auth.password.min_length", "8", "8", ConfigType.System, ConfigDataType.Number, "账号密码策略的最小长度基线", 60),
            new("密码要求数字", "auth", "auth.password.require_digit", "true", "true", ConfigType.System, ConfigDataType.Boolean, "账号密码策略是否要求至少包含一个数字", 70),
            new("密码要求大写字母", "auth", "auth.password.require_uppercase", "true", "true", ConfigType.System, ConfigDataType.Boolean, "账号密码策略是否要求至少包含一个大写字母", 80),
            new("允许多设备登录", "auth", "auth.session.allow_multi_login", "true", "true", ConfigType.System, ConfigDataType.Boolean, "默认登录策略是否允许同一用户多设备在线", 90),
            new("默认最大登录设备", "auth", "auth.session.max_login_devices", "5", "5", ConfigType.System, ConfigDataType.Number, "普通账号默认最大在线设备数，0 表示不限", 100),
            new("账号锁定分钟数", "security", "security.account_lockout_minutes", "30", "30", ConfigType.System, ConfigDataType.Number, "触发登录风控后默认锁定时长", 110),
            new("审计日志保留天数", "audit", "audit.log_retention_days", "180", "180", ConfigType.System, ConfigDataType.Number, "审计与访问日志默认保留周期", 120)
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
        int Sort);
}
