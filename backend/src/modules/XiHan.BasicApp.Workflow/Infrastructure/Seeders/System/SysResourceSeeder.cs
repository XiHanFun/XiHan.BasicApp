#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResourceSeeder
// Guid:6a05c93e-27d1-4b84-a6f0-91e58d23c7b5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:33:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;

/// <summary>
/// 系统资源种子数据
/// </summary>
public class SysResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysResourceSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（工作流种子统一在 Order 300+ 独立段；须先于 SysPermissionSeeder）
    /// </summary>
    public override int Order => 301;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Workflow]系统资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysResource>()
            .Where(r => r.ResourceCode == WorkflowPermissionCodes.Resource)
            .ToListAsync();
        if (exists.Count > 0)
        {
            Logger.LogInformation("系统资源数据已存在，跳过种子数据");
            return;
        }

        var addList = new List<SysResource>
        {
            new()
            {
                ResourceCode = WorkflowPermissionCodes.Resource,
                ResourceName = "工作流",
                ResourceType = ResourceType.Api,
                ResourcePath = "/api/workflow",
                Description = "工作流服务API接口",
                AccessLevel = ResourceAccessLevel.Authorized,
                Status = EnableStatus.Enabled,
                Sort = 403
            }
        };

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统资源", addList.Count);
    }
}
