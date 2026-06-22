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
        // 仅保留运行期被强类型读取入口（SaasConfigurationService.GetLoginConfigAsync）实际消费的配置键。
        // 其余历史占位键未被任何代码引用，已移除以避免无效内置参数（详见配置审计）。
        return
        [
            new("登录方式", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.LoginMethods, "[\"password\"]", "[\"password\"]", ConfigType.Feature, ConfigDataType.Array, "登录页开放的登录方式编码集合", 50),
            new("OAuth 提供商", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.OAuthProviders, "[{\"name\":\"github\",\"displayName\":\"Github\"},{\"name\":\"google\",\"displayName\":\"Google\"},{\"name\":\"qq\",\"displayName\":\"QQ\"}]", "[]", ConfigType.Feature, ConfigDataType.Array, "登录页展示的 OAuth 提供商 JSON 数组", 70)
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
