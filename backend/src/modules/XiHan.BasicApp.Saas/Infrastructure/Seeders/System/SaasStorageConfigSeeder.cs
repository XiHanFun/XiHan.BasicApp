// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 存储配置种子数据
/// </summary>
public sealed class SaasStorageConfigSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasStorageConfigSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 24;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]存储配置种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;

        var existing = await client.Queryable<SysStorageConfig>()
            .Where(config => config.TenantId == 0 && config.ConfigCode == "local-default" && !config.IsDeleted)
            .FirstAsync();
        if (existing is not null)
        {
            if (ApplyDefaultStorageConfig(existing))
            {
                _ = await client.Updateable(existing).ExecuteCommandAsync();
                Logger.LogInformation("默认存储配置已更新");
                return;
            }

            Logger.LogInformation("默认存储配置已存在，跳过种子数据");
            return;
        }

        var config = new SysStorageConfig
        {
            ConfigCode = "local-default"
        };
        _ = ApplyDefaultStorageConfig(config);

        _ = await client.Insertable(config).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化默认存储配置");
    }

    private static bool ApplyDefaultStorageConfig(SysStorageConfig config)
    {
        var changed = false;
        changed |= SetIfChanged(config.TenantId, 0, value => config.TenantId = value);
        changed |= SetIfChanged(config.ConfigCode, "local-default", value => config.ConfigCode = value);
        changed |= SetIfChanged(config.ConfigName, "本地存储", value => config.ConfigName = value);
        changed |= SetIfChanged(config.StorageType, StorageConfigType.Local, value => config.StorageType = value);
        changed |= SetIfChanged(config.IsDefault, true, value => config.IsDefault = value);
        changed |= SetIfChanged(config.IsEnabled, true, value => config.IsEnabled = value);
        changed |= SetIfChanged(config.Sort, 10, value => config.Sort = value);
        changed |= SetIfChanged(config.Remark, "系统初始化默认本地存储配置", value => config.Remark = value);
        return changed;
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
}
