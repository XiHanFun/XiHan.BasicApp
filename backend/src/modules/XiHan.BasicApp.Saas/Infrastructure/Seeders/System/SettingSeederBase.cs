// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// 一条参数配置的种子声明
/// </summary>
/// <param name="Key">配置键</param>
/// <param name="Name">配置名称</param>
/// <param name="Group">配置分组</param>
/// <param name="DataType">值类型</param>
/// <param name="Value">初始值（新建时写入）</param>
/// <param name="DefaultValue">默认值（未配置时运行所用的值，展示给运营参考）</param>
/// <param name="Description">说明</param>
/// <param name="Sort">排序</param>
/// <param name="IsEncrypted">是否加密存储</param>
public sealed record SettingSeed(
    string Key,
    string Name,
    string Group,
    ConfigDataType DataType,
    string Value,
    string DefaultValue,
    string Description,
    int Sort,
    bool IsEncrypted = false)
{
    /// <summary>
    /// JSON 设置：初始值与默认值都由设置类型序列化，结构只在类型里定义一处
    /// </summary>
    public static SettingSeed Json<T>(string key, string name, string group, T value, T defaultValue, string description, int sort)
    {
        return new SettingSeed(key, name, group, ConfigDataType.Json, Serialize(value), Serialize(defaultValue), description, sort);
    }

    /// <summary>
    /// 数字设置
    /// </summary>
    public static SettingSeed Number(string key, string name, string group, int value, string description, int sort)
    {
        var text = value.ToString(CultureInfo.InvariantCulture);
        return new SettingSeed(key, name, group, ConfigDataType.Number, text, text, description, sort);
    }

    /// <summary>
    /// 加密存储的文本设置（初始为空，由运营填写）
    /// </summary>
    public static SettingSeed Secret(string key, string name, string group, string description, int sort)
    {
        return new SettingSeed(key, name, group, ConfigDataType.String, string.Empty, string.Empty, description, sort, IsEncrypted: true);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SaasConfigurationService.JsonOptions);
    }
}

/// <summary>
/// 参数配置种子基类：按声明写入平台的参数配置（TenantId = 0）
/// </summary>
/// <remarks>
/// 配置值与启停归运营：已有的配置只对齐名称、分组、类型、说明、默认值、排序这些由代码定义的元数据，
/// 配置值只在为空时补上初始值，不覆盖运营改过的值，也不动启停状态；缺的配置按声明插入。
/// </remarks>
public abstract class SettingSeederBase(
    ISqlSugarClientResolver clientResolver,
    ILogger logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 内置配置的备注
    /// </summary>
    private const string SeededRemark = "系统初始化内置参数";

    /// <summary>
    /// 本模块的参数配置声明
    /// </summary>
    public abstract IReadOnlyList<SettingSeed> Settings { get; }

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var protector = ServiceProvider.GetRequiredService<IConfigValueSecretProtector>();
        var keys = Settings.Select(static setting => setting.Key).ToList();
        var existing = (await DbClient.Queryable<SysConfig>()
                .Where(config => config.TenantId == 0 && keys.Contains(config.ConfigKey))
                .ToListAsync())
            .ToDictionary(config => config.ConfigKey, StringComparer.OrdinalIgnoreCase);

        var adding = new List<SysConfig>();
        var updated = 0;
        foreach (var setting in Settings)
        {
            var value = setting.IsEncrypted && setting.Value.Length > 0 ? protector.Protect(setting.Value)! : setting.Value;
            if (!existing.TryGetValue(setting.Key, out var config))
            {
                config = new SysConfig { ConfigKey = setting.Key, ConfigValue = value, Status = EnableStatus.Enabled };
                _ = ApplyMetadata(config, setting);
                adding.Add(config);
                continue;
            }

            var changed = ApplyMetadata(config, setting);
            if (string.IsNullOrWhiteSpace(config.ConfigValue) && value.Length > 0)
            {
                config.ConfigValue = value;
                changed = true;
            }

            if (changed)
            {
                _ = await DbClient.Updateable(config).ExecuteCommandAsync();
                updated++;
            }
        }

        if (adding.Count > 0)
        {
            await BulkInsertAsync(adding);
        }

        Logger.LogInformation("{Seeder}：新增 {AddCount} 条、对齐 {UpdateCount} 条参数配置", Name, adding.Count, updated);
    }

    /// <summary>
    /// 对齐由代码定义的元数据
    /// </summary>
    private static bool ApplyMetadata(SysConfig config, SettingSeed setting)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(config.ConfigName, setting.Name, value => config.ConfigName = value);
        changed |= SeedValues.SetIfChanged(config.ConfigGroup, setting.Group, value => config.ConfigGroup = value);
        changed |= SeedValues.SetIfChanged(config.ConfigType, ConfigType.Feature, value => config.ConfigType = value);
        changed |= SeedValues.SetIfChanged(config.DataType, setting.DataType, value => config.DataType = value);
        changed |= SeedValues.SetIfChanged(config.DefaultValue, setting.DefaultValue, value => config.DefaultValue = value);
        changed |= SeedValues.SetIfChanged(config.ConfigDescription, setting.Description, value => config.ConfigDescription = value);
        changed |= SeedValues.SetIfChanged(config.IsBuiltIn, true, value => config.IsBuiltIn = value);
        changed |= SeedValues.SetIfChanged(config.IsEncrypted, setting.IsEncrypted, value => config.IsEncrypted = value);
        changed |= SeedValues.SetIfChanged(config.Sort, setting.Sort, value => config.Sort = value);
        changed |= SeedValues.SetIfChanged(config.Remark, SeededRemark, value => config.Remark = value);
        return changed;
    }
}
