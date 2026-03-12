#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacSettingStore
// Guid:a37f0a12-3af2-4f31-a308-2cf4c7be58f0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 14:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Settings.Definitions;
using XiHan.Framework.Settings.Stores;

namespace XiHan.BasicApp.Saas.Infrastructure.Settings;

/// <summary>
/// 基于 SysConfig 的设置存储实现
/// </summary>
public class RbacSettingStore : ISettingStore
{
    private const string SettingConfigGroup = "Framework.Settings";
    private const string GlobalProviderName = "G";
    private const string UserProviderName = "U";

    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configRepository"></param>
    public RbacSettingStore(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    /// <summary>
    /// 获取设置值
    /// </summary>
    /// <param name="name">设置名称</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="providerKey">提供者键</param>
    /// <returns></returns>
    public async Task<string?> GetOrNullAsync(string name, string? providerName, string? providerKey)
    {
        var key = BuildConfigKey(name, providerName, providerKey);
        var config = await _configRepository.GetByConfigKeyAsync(key, null);
        return config?.ConfigValue;
    }

    /// <summary>
    /// 获取所有设置值
    /// </summary>
    /// <param name="names">设置名称数组</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="providerKey">提供者键</param>
    /// <returns></returns>
    public async Task<List<SettingValue>> GetAllAsync(string[] names, string? providerName, string? providerKey)
    {
        var result = new List<SettingValue>(names.Length);
        foreach (var name in names)
        {
            var value = await GetOrNullAsync(name, providerName, providerKey);
            result.Add(new SettingValue(name, value));
        }

        return result;
    }

    /// <summary>
    /// 设置值
    /// </summary>
    /// <param name="name">设置名称</param>
    /// <param name="value">设置值</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="providerKey">提供者键</param>
    /// <returns></returns>
    public async Task SetAsync(string name, string? value, string? providerName, string? providerKey)
    {
        var key = BuildConfigKey(name, providerName, providerKey);
        var config = await _configRepository.GetByConfigKeyAsync(key, null);

        if (config is null)
        {
            config = new SysConfig
            {
                ConfigName = NormalizeConfigName(name),
                ConfigGroup = SettingConfigGroup,
                ConfigKey = key,
                ConfigValue = value,
                ConfigType = ResolveConfigType(providerName),
                DataType = ConfigDataType.String,
                ConfigDescription = BuildDescription(name, providerName, providerKey),
                IsBuiltIn = false,
                IsEncrypted = false,
                Status = YesOrNo.Yes
            };

            await _configRepository.AddAsync(config);
            return;
        }

        config.ConfigValue = value;
        config.ConfigName = NormalizeConfigName(name);
        config.ConfigType = ResolveConfigType(providerName);
        config.ConfigDescription = BuildDescription(name, providerName, providerKey);
        config.Status = YesOrNo.Yes;
        await _configRepository.UpdateAsync(config);
    }

    /// <summary>
    /// 删除设置值
    /// </summary>
    /// <param name="name">设置名称</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="providerKey">提供者键</param>
    /// <returns></returns>
    public async Task DeleteAsync(string name, string? providerName, string? providerKey)
    {
        var key = BuildConfigKey(name, providerName, providerKey);
        var config = await _configRepository.GetByConfigKeyAsync(key, null);
        if (config is null)
        {
            return;
        }

        await _configRepository.DeleteAsync(config);
    }

    private static string BuildConfigKey(string name, string? providerName, string? providerKey)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        var normalizedProviderName = string.IsNullOrWhiteSpace(providerName) ? GlobalProviderName : providerName.Trim().ToUpperInvariant();
        var normalizedProviderKey = providerKey?.Trim() ?? string.Empty;
        var rawKey = $"{normalizedProviderName}|{normalizedProviderKey}|{normalizedName}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        return $"set:{hash}";
    }

    private static string NormalizeConfigName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "FrameworkSetting";
        }

        var normalized = name.Trim();
        return normalized.Length <= 100 ? normalized : normalized[..100];
    }

    private static ConfigType ResolveConfigType(string? providerName)
    {
        return string.Equals(providerName, UserProviderName, StringComparison.OrdinalIgnoreCase)
            ? ConfigType.User
            : ConfigType.System;
    }

    private static string BuildDescription(string name, string? providerName, string? providerKey)
    {
        var normalizedProviderName = string.IsNullOrWhiteSpace(providerName) ? GlobalProviderName : providerName.Trim().ToUpperInvariant();
        var normalizedProviderKey = string.IsNullOrWhiteSpace(providerKey) ? "-" : providerKey.Trim();
        var description = $"Setting:{name};Provider:{normalizedProviderName};Key:{normalizedProviderKey}";
        return description.Length <= 500 ? description : description[..500];
    }
}
