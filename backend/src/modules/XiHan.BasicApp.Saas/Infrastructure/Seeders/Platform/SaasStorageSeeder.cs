// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 存储：平台默认的本地存储
/// </summary>
/// <remarks>
/// 文件上传要有一个默认存储才能用。存储配置归运营（改默认、换对象存储、停用），只在首次创建时写入，之后不再覆盖；
/// 运营删掉的也不补回。
/// </remarks>
public sealed class SaasStorageSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasStorageSeeder> logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 默认本地存储的编码
    /// </summary>
    public const string LocalDefaultCode = "local-default";

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData + 1;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]默认存储";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await DbClient.Queryable<SysStorageConfig>()
                .IncludingDeleted()
                .AnyAsync(config => config.TenantId == 0 && config.ConfigCode == LocalDefaultCode))
        {
            Logger.LogInformation("{Seeder}：已存在，不覆盖运营的修改", Name);
            return;
        }

        _ = await DbClient.Insertable(new SysStorageConfig
        {
            TenantId = 0,
            ConfigCode = LocalDefaultCode,
            ConfigName = "本地存储",
            StorageType = StorageConfigType.Local,
            IsDefault = true,
            IsEnabled = true,
            Sort = 10,
            Remark = "系统初始化默认本地存储"
        }).ExecuteCommandAsync();
        Logger.LogInformation("{Seeder}：新增默认本地存储", Name);
    }
}
