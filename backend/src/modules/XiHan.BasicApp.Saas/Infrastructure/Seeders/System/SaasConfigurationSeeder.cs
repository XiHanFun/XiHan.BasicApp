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
using XiHan.BasicApp.Saas.Domain.Identity;
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
        return
        [
            new("应用名称", SaasConfigKeys.Groups.Application, SaasConfigKeys.Application.Name, "XiHan BasicApp", "XiHan BasicApp", ConfigType.Application, ConfigDataType.String, "前端标题、通知和系统标识使用的应用名称", 10),
            new("应用 Logo", SaasConfigKeys.Groups.Application, SaasConfigKeys.Application.Logo, string.Empty, string.Empty, ConfigType.Application, ConfigDataType.String, "前端和通知场景使用的应用 Logo 地址", 20),
            new("默认语言", SaasConfigKeys.Groups.Application, SaasConfigKeys.Application.DefaultLanguage, "zh-CN", "zh-CN", ConfigType.Application, ConfigDataType.String, "系统默认语言区域", 30),
            new("默认租户版本", SaasConfigKeys.Groups.Tenant, SaasConfigKeys.Tenant.DefaultEditionCode, "free", "free", ConfigType.Tenant, ConfigDataType.String, "新租户未显式选择版本时使用的版本编码", 40),
            new("登录方式", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.LoginMethods, "[\"password\"]", "[\"password\"]", ConfigType.Feature, ConfigDataType.Array, "登录页开放的登录方式编码集合", 50),
            new("租户选择开关", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.TenantSelectionEnabled, "true", "true", ConfigType.Feature, ConfigDataType.Boolean, "登录页是否允许选择租户上下文", 60),
            new("OAuth 提供商", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.OAuthProviders, "[{\"name\":\"github\",\"displayName\":\"Github\"},{\"name\":\"google\",\"displayName\":\"Google\"},{\"name\":\"qq\",\"displayName\":\"QQ\"}]", "[]", ConfigType.Feature, ConfigDataType.Array, "登录页展示的 OAuth 提供商 JSON 数组", 70),
            new("登录通知开关", SaasConfigKeys.Groups.Notification, SaasConfigKeys.Notification.AuthLoginEnabled, "true", "true", ConfigType.Feature, ConfigDataType.Boolean, "登录成功后是否写入并推送当前用户通知", 80),
            new("登出通知开关", SaasConfigKeys.Groups.Notification, SaasConfigKeys.Notification.AuthLogoutEnabled, "true", "true", ConfigType.Feature, ConfigDataType.Boolean, "主动退出后是否写入并推送当前用户通知", 90),
            new("密码最小长度", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.PasswordMinLength, "8", "8", ConfigType.Feature, ConfigDataType.Number, "账号密码策略的最小长度基线", 100),
            new("密码要求数字", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.PasswordRequireDigit, "true", "true", ConfigType.Feature, ConfigDataType.Boolean, "账号密码策略是否要求至少包含一个数字", 110),
            new("密码要求大写字母", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.PasswordRequireUppercase, "true", "true", ConfigType.Feature, ConfigDataType.Boolean, "账号密码策略是否要求至少包含一个大写字母", 120),
            new("最大登录失败次数", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.PasswordMaxFailedAttempts, "5", "5", ConfigType.Feature, ConfigDataType.Number, "账号被锁定前允许的连续密码错误次数", 130),
            new("账号锁定分钟数", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.AccountLockoutMinutes, "15", "15", ConfigType.Feature, ConfigDataType.Number, "触发登录风控后默认锁定时长", 140),
            new("允许多设备登录", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.SessionAllowMultiLogin, "true", "true", ConfigType.Feature, ConfigDataType.Boolean, "默认登录策略是否允许同一用户多设备在线", 150),
            new("默认最大登录设备", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.SessionMaxLoginDevices, "5", "5", ConfigType.Feature, ConfigDataType.Number, "普通账号默认最大在线设备数，0 表示不限", 160),
            new("默认 OAuth ClientId", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.SessionClientId, SaasOAuthClientIds.Web, SaasOAuthClientIds.Web, ConfigType.Feature, ConfigDataType.String, "密码登录签发 OAuth Token 时使用的默认 ClientId", 170),
            new("默认 OAuth Scope", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.SessionScope, SaasOAuthClientIds.DefaultScope, SaasOAuthClientIds.DefaultScope, ConfigType.Feature, ConfigDataType.String, "密码登录签发 OAuth Token 时使用的默认 Scope", 180),
            new("刷新令牌有效天数", SaasConfigKeys.Groups.Auth, SaasConfigKeys.Auth.SessionRefreshTokenDays, "7", "7", ConfigType.Feature, ConfigDataType.Number, "密码登录签发刷新令牌的有效天数", 190),
            new("审计日志保留天数", SaasConfigKeys.Groups.Audit, SaasConfigKeys.Audit.LogRetentionDays, "180", "180", ConfigType.Environment, ConfigDataType.Number, "审计与访问日志默认保留周期", 200),
            new("文件存储提供者", SaasConfigKeys.Groups.File, SaasConfigKeys.File.StorageProvider, "local", "local", ConfigType.Environment, ConfigDataType.String, "文件存储提供者类型：local / s3 / oss / azure", 210),
            new("文件存储根路径", SaasConfigKeys.Groups.File, SaasConfigKeys.File.StorageBasePath, "uploads", "uploads", ConfigType.Environment, ConfigDataType.String, "本地文件存储根路径或云存储 Bucket 前缀", 220),
            new("文件上传最大大小(MB)", SaasConfigKeys.Groups.File, SaasConfigKeys.File.MaxFileSize, "50", "50", ConfigType.Environment, ConfigDataType.Number, "单次上传文件的最大大小限制(MB)", 230),
            new("文件允许扩展名", SaasConfigKeys.Groups.File, SaasConfigKeys.File.AllowedExtensions, ".jpg,.jpeg,.png,.gif,.webp,.svg,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.csv,.zip,.rar", ".jpg,.jpeg,.png,.gif,.webp,.svg,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.csv,.zip,.rar", ConfigType.Environment, ConfigDataType.String, "允许上传的文件扩展名列表，逗号分隔", 240),
            new("文件有效期天数", SaasConfigKeys.Groups.File, SaasConfigKeys.File.FileExpiryDays, "0", "0", ConfigType.Environment, ConfigDataType.Number, "文件存储过期天数，0 表示永不过期", 250),
            new("文件存储路由映射", SaasConfigKeys.Groups.File, SaasConfigKeys.File.StorageRoutes, "[{\"route\":\"avatar\",\"provider\":\"local\"},{\"route\":\"document\",\"provider\":\"minio\"}]", "[{\"route\":\"avatar\",\"provider\":\"local\"},{\"route\":\"document\",\"provider\":\"minio\"}]", ConfigType.Environment, ConfigDataType.Array, "文件类别到存储提供者的路由映射 JSON，格式：[{\"route\":\"avatar\",\"provider\":\"local\"}]", 260)
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
