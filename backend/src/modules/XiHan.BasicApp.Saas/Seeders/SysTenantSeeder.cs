#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantSeeder
// Guid:5a3ff7e7-9cb6-4f25-8d5f-0fd2fdaf7b65
// Author:zhaifanhua
// CreateTime:2026/03/13 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统租户种子数据
/// </summary>
public class SysTenantSeeder : DataSeederBase
{
    private const string DefaultTenantCode = "DEFAULT";

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTenantSeeder(ISqlSugarDbContext dbContext, ILogger<SysTenantSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 9;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]系统租户种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        // 仅保证默认租户存在；若已有其他租户，仍可补齐默认租户。
        if (await HasDataAsync<SysTenant>(tenant => tenant.TenantCode == DefaultTenantCode))
        {
            Logger.LogInformation("默认租户数据已存在，跳过种子数据");
            return;
        }

        var tenants = new List<SysTenant>
        {
            new()
            {
                TenantCode = DefaultTenantCode,
                TenantName = "默认租户",
                TenantShortName = "默认",
                ContactPerson = "系统管理员",
                ContactPhone = "13800138000",
                ContactEmail = "admin@xihanfun.com",
                IsolationMode = TenantIsolationMode.Field,
                ConfigStatus = TenantConfigStatus.Configured,
                ExpireTime = DateTimeOffset.UtcNow.AddYears(30),
                UserLimit = 10000,
                StorageLimit = 102400,
                TenantStatus = TenantStatus.Normal,
                Status = YesOrNo.Yes,
                Sort = 0,
                Remark = "默认租户"
            }
        };

        await BulkInsertAsync(tenants);
        Logger.LogInformation("成功初始化 {Count} 个租户", tenants.Count);
    }
}
