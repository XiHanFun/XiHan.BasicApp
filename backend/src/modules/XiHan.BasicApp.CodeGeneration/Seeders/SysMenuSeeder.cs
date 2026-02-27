#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuSeeder
// Guid:0a1eb7e6-a46a-4e5e-a76d-df9a0f5c4964
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 13:11:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Seeders;

/// <summary>
/// 系统菜单种子数据
/// </summary>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarDbContext dbContext, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider) : base(dbContext, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 21;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbContext.GetClient();
        var resources = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "develop" || r.ResourceCode == "code_gen").ToListAsync();
        var resourceMap = resources.ToDictionary(r => r.ResourceCode, r => r.BasicId);
        if (!resourceMap.ContainsKey("develop") || !resourceMap.ContainsKey("code_gen"))
        {
            Logger.LogWarning("系统资源不存在，跳过系统菜单种子数据");
            return;
        }

        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "code_gen").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();
        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ResourceId = resourceMap["develop"], ParentId = null, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "tool", Title = "开发工具", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 400 });
        }

        if (!existsCodes.Contains("code_gen"))
        {
            addList.Add(new SysMenu { ResourceId = resourceMap["code_gen"], ParentId = null, MenuName = "代码生成", MenuCode = "code_gen", MenuType = MenuType.Menu, Path = "/develop/codeGen", Component = "Develop/CodeGen/Index", RouteName = "DevelopCodeGen", Icon = "code", Title = "代码生成", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 401 });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统菜单数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        var parentMenu = await client.Queryable<SysMenu>().FirstAsync(m => m.MenuCode == "develop");
        if (parentMenu != null)
        {
            await client.Updateable<SysMenu>().SetColumns(m => m.ParentId == parentMenu.BasicId).Where(m => m.MenuCode == "code_gen").ExecuteCommandAsync();
        }

        Logger.LogInformation("成功初始化 {Count} 个系统菜单", addList.Count);
    }
}
