#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantSeeder
// Guid:5a3ff7e7-9cb6-4f25-8d5f-0fd2fdaf7b65
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/13 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统租户种子数据。
/// </summary>
public class SysTenantSeeder : DataSeederBase
{
    public SysTenantSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysTenantSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Tenants;

    public override string Name => "[Saas]系统租户种子数据";

    protected override async Task SeedInternalAsync()
    {
        var template = new SysTenant
        {
            TenantCode = SaasSeedDefaults.BootstrapTenantCode,
            TenantName = SaasSeedDefaults.BootstrapTenantName,
            TenantShortName = "默认",
            ContactPerson = "系统管理员",
            ContactPhone = "13800138000",
            ContactEmail = "admin@xihanfun.com",
            IsolationMode = TenantIsolationMode.Field,
            ConfigStatus = TenantConfigStatus.Configured,
            ExpireTime = DateTimeOffset.UtcNow.AddYears(30),
            UserLimit = 200,
            StorageLimit = 102400,
            TenantStatus = TenantStatus.Normal,
            Sort = 0,
            Remark = "系统初始化默认租户"
        };

        var existingTenant = await DbClient
            .Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == template.TenantCode);

        if (existingTenant is null)
        {
            await BulkInsertAsync([template]);
            Logger.LogInformation("默认租户种子完成：新增租户 {TenantCode}", template.TenantCode);
            return;
        }

        if (existingTenant.TenantStatus != TenantStatus.Normal || existingTenant.ConfigStatus != TenantConfigStatus.Configured)
        {
            await DbClient
                .Updateable<SysTenant>()
                .SetColumns(tenant => new SysTenant
                {
                    TenantStatus = TenantStatus.Normal,
                    ConfigStatus = TenantConfigStatus.Configured
                })
                .Where(tenant => tenant.BasicId == existingTenant.BasicId)
                .ExecuteCommandAsync();
        }

        Logger.LogInformation("默认租户已存在，保留现有租户资料并完成状态校准");
    }
}
