#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResourceSeeder
// Guid:7f22adcd-93d8-41e0-bf6b-337dc4cf3e81
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 13:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Seeders;

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
    /// 种子数据优先级
    /// </summary>
    public override int Order => 30;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "code_gen").ToListAsync();
        var existsCodes = exists.Select(x => x.ResourceCode).ToHashSet();
        var addList = new List<SysResource>();

        if (!existsCodes.Contains("code_gen"))
        {
            addList.Add(new SysResource { ResourceCode = "code_gen", ResourceName = "代码生成", ResourceType = ResourceType.Api, ResourcePath = "/api/codegen", Description = "代码生成API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 401 });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统资源数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统资源", addList.Count);
    }
}
