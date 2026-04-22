#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigSeeder
// Guid:be6ad33d-b678-4682-ab57-f5e246f24b76
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 12:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Constants.Settings;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统配置种子数据
/// </summary>
public class SysConfigSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysConfigSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysConfigSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SaasSeedOrder.Configs;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]系统配置种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var configTemplates = new List<SysConfig>
        {
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "登录方式",
                ConfigGroup = "BasicApp.Saas.Auth",
                ConfigKey = SaasSettingKeys.Auth.LoginMethods,
                ConfigValue = "Password,PhoneCode,EmailCode",
                DefaultValue = "Password",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "平台级登录方式白名单",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 1
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "认证调试敏感信息",
                ConfigGroup = "BasicApp.Saas.Auth",
                ConfigKey = SaasSettingKeys.Auth.ExposeDebugSecrets,
                ConfigValue = "false",
                DefaultValue = "false",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Boolean,
                ConfigDescription = "仅限开发环境显式开启",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 2
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "手机验证码过期秒数",
                ConfigGroup = "BasicApp.Saas.Auth",
                ConfigKey = SaasSettingKeys.Auth.PhoneCodeExpiresInSeconds,
                ConfigValue = "300",
                DefaultValue = "300",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "手机验证码默认有效期",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 3
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "邮箱验证码过期秒数",
                ConfigGroup = "BasicApp.Saas.Auth",
                ConfigKey = SaasSettingKeys.Auth.EmailCodeExpiresInSeconds,
                ConfigValue = "300",
                DefaultValue = "300",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "邮箱验证码默认有效期",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 4
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "双因素验证码过期秒数",
                ConfigGroup = "BasicApp.Saas.Auth",
                ConfigKey = SaasSettingKeys.Auth.TwoFactorCodeExpiresInSeconds,
                ConfigValue = "300",
                DefaultValue = "300",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "双因素验证码默认有效期",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 5
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "多租户开关",
                ConfigGroup = "BasicApp.Saas.MultiTenancy",
                ConfigKey = SaasSettingKeys.MultiTenancy.Enabled,
                ConfigValue = "true",
                DefaultValue = "true",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Boolean,
                ConfigDescription = "控制 SaaS 多租户模式是否启用",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 10
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "授权缓存绝对过期分钟",
                ConfigGroup = "BasicApp.Saas.Caching",
                ConfigKey = SaasSettingKeys.Caching.AuthorizationAbsoluteExpirationMinutes,
                ConfigValue = "30",
                DefaultValue = "30",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "授权聚合缓存绝对过期时间",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 20
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "授权缓存滑动过期分钟",
                ConfigGroup = "BasicApp.Saas.Caching",
                ConfigKey = SaasSettingKeys.Caching.AuthorizationSlidingExpirationMinutes,
                ConfigValue = "10",
                DefaultValue = "10",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "授权聚合缓存滑动过期时间",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 21
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "查询缓存绝对过期分钟",
                ConfigGroup = "BasicApp.Saas.Caching",
                ConfigKey = SaasSettingKeys.Caching.LookupAbsoluteExpirationMinutes,
                ConfigValue = "20",
                DefaultValue = "20",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "Lookup 缓存绝对过期时间",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 22
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "查询缓存滑动过期分钟",
                ConfigGroup = "BasicApp.Saas.Caching",
                ConfigKey = SaasSettingKeys.Caching.LookupSlidingExpirationMinutes,
                ConfigValue = "5",
                DefaultValue = "5",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "Lookup 缓存滑动过期时间",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 23
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "消息未读缓存绝对过期分钟",
                ConfigGroup = "BasicApp.Saas.Caching",
                ConfigKey = SaasSettingKeys.Caching.MessageUnreadAbsoluteExpirationMinutes,
                ConfigValue = "5",
                DefaultValue = "5",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "消息未读缓存绝对过期时间",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 24
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "消息未读缓存滑动过期分钟",
                ConfigGroup = "BasicApp.Saas.Caching",
                ConfigKey = SaasSettingKeys.Caching.MessageUnreadSlidingExpirationMinutes,
                ConfigValue = "2",
                DefaultValue = "2",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "消息未读缓存滑动过期时间",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 25
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "启用演示数据",
                ConfigGroup = "BasicApp.Saas.Seed",
                ConfigKey = SaasSettingKeys.Seed.EnableDemoData,
                ConfigValue = "false",
                DefaultValue = "false",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Boolean,
                ConfigDescription = "显式开启后才允许执行演示/测试数据 Seeder",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 30
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "基线租户编码",
                ConfigGroup = "BasicApp.Saas.Seed",
                ConfigKey = SaasSettingKeys.Seed.BootstrapTenantCode,
                ConfigValue = SaasSeedDefaults.BootstrapTenantCode,
                DefaultValue = SaasSeedDefaults.BootstrapTenantCode,
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "租户初始化链默认租户编码",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 31
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "基线租户名称",
                ConfigGroup = "BasicApp.Saas.Seed",
                ConfigKey = SaasSettingKeys.Seed.BootstrapTenantName,
                ConfigValue = SaasSeedDefaults.BootstrapTenantName,
                DefaultValue = SaasSeedDefaults.BootstrapTenantName,
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "租户初始化链默认租户名称",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 32
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                IsGlobal = true,
                ConfigName = "基线超管账号",
                ConfigGroup = "BasicApp.Saas.Seed",
                ConfigKey = SaasSettingKeys.Seed.BootstrapAdminUserName,
                ConfigValue = SaasSeedDefaults.BootstrapAdminUserName,
                DefaultValue = SaasSeedDefaults.BootstrapAdminUserName,
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "平台初始化链默认超管用户名",
                IsBuiltIn = true,
                Status = YesOrNo.Yes,
                Sort = 33
            }
        };

        var configKeys = configTemplates.Select(config => config.ConfigKey).Distinct().ToArray();
        var existingConfigs = await DbClient
            .Queryable<SysConfig>()
            .Where(config => configKeys.Contains(config.ConfigKey))
            .ToListAsync();

        var existingConfigMap = existingConfigs.ToDictionary(config => config.ConfigKey, StringComparer.OrdinalIgnoreCase);
        var configsToInsert = configTemplates
            .Where(config => !existingConfigMap.ContainsKey(config.ConfigKey))
            .ToList();

        if (configsToInsert.Count > 0)
        {
            await BulkInsertAsync(configsToInsert);
        }

        var configIdsToNormalize = existingConfigs
            .Where(config =>
                config.Status != YesOrNo.Yes
                || !config.IsGlobal
                || !config.IsBuiltIn
                || config.TenantId != SaasSeedDefaults.PlatformTenantId)
            .Select(config => config.BasicId)
            .Distinct()
            .ToArray();

        if (configIdsToNormalize.Length > 0)
        {
            await DbClient
                .Updateable<SysConfig>()
                .SetColumns(config => new SysConfig
                {
                    TenantId = SaasSeedDefaults.PlatformTenantId,
                    IsGlobal = true,
                    IsBuiltIn = true,
                    Status = YesOrNo.Yes,
                    ModifiedTime = now
                })
                .Where(config => configIdsToNormalize.Contains(config.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统配置种子完成：新增 {InsertCount} 项统一配置，校准 {NormalizeCount} 项平台配置元数据",
            configsToInsert.Count,
            configIdsToNormalize.Length);
    }
}
