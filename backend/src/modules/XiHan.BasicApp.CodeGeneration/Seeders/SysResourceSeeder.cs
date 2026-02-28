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
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
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
    public SysResourceSeeder(ISqlSugarClientProvider clientProvider, ILogger<SysResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientProvider, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 20;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = ClientProvider.GetClient();
        var exists = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "develop" || r.ResourceCode == "code_gen" || r.ResourceCode == "code_gen_api").ToListAsync();
        var existsCodes = exists.Select(x => x.ResourceCode).ToHashSet();
        var addList = new List<SysResource>();
        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysResource { ParentId = null, ResourceCode = "develop", ResourceName = "开发工具", ResourceType = ResourceType.Menu, ResourcePath = "/develop", Icon = "tool", Description = "开发工具目录", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 400 });
        }

        if (!existsCodes.Contains("code_gen"))
        {
            addList.Add(new SysResource { ParentId = null, ResourceCode = "code_gen", ResourceName = "代码生成", ResourceType = ResourceType.Menu, ResourcePath = "/develop/codeGen", Icon = "code", Description = "代码生成功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 401 });
        }

        if (!existsCodes.Contains("code_gen_api"))
        {
            addList.Add(new SysResource { ParentId = null, ResourceCode = "code_gen_api", ResourceName = "代码生成API", ResourceType = ResourceType.Api, ResourcePath = "/api/codegen", Description = "代码生成API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 509 });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统资源数据已存在，跳过种子数据"); return;
        }
        await BulkInsertAsync(addList);
        var parent = await client.Queryable<SysResource>().FirstAsync(r => r.ResourceCode == "develop");
        if (parent != null)
        {
            await client.Updateable<SysResource>().SetColumns(r => r.ParentId == parent.BasicId).Where(r => r.ResourceCode == "code_gen").ExecuteCommandAsync();
        }

        Logger.LogInformation("成功初始化 {Count} 个系统资源", addList.Count);
    }
}
