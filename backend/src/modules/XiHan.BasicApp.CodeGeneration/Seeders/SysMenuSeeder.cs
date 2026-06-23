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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
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
    public SysMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider) : base(clientResolver, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 31;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "code_gen").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ParentId = null, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", I18nKey = "menu.develop", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 400, Remark = "开发工具目录" });
        }

        if (!existsCodes.Contains("code_gen"))
        {
            addList.Add(new SysMenu { ParentId = null, MenuName = "代码生成", MenuCode = "code_gen", MenuType = MenuType.Menu, Path = "/develop/codeGen", Component = "Develop/CodeGen/Index", RouteName = "DevelopCodeGen", Icon = "lucide:code-xml", Title = "代码生成", I18nKey = "menu.code_gen", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 401, Remark = "代码生成" });
        }

        if (addList.Count > 0)
        {
            await BulkInsertAsync(addList);
        }

        var parentMenu = await client.Queryable<SysMenu>().FirstAsync(m => m.MenuCode == "develop");
        if (parentMenu != null)
        {
            await client.Updateable<SysMenu>()
                .SetColumns(m => m.ParentId == parentMenu.BasicId)
                .Where(m => m.MenuCode == "code_gen")
                .ExecuteCommandAsync();
        }

        // 回填国际化键：存量行（已建库）也补上 I18nKey，菜单标题由前端按该键翻译（te 命中则译，否则回退原文）。
        // 按 MenuCode 等值分别更新，避免可空列 OR 在 PostgreSQL 上的翻译问题。
        await client.Updateable<SysMenu>().SetColumns(m => m.I18nKey == "menu.develop").Where(m => m.MenuCode == "develop").ExecuteCommandAsync();
        await client.Updateable<SysMenu>().SetColumns(m => m.I18nKey == "menu.code_gen").Where(m => m.MenuCode == "code_gen").ExecuteCommandAsync();

        // 代码生成前端与应用服务已就绪，菜单解除隐藏：把仍隐藏/停用的项置为可见且启用
        var showCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "develop" || m.MenuCode == "code_gen")
            .Where(m => !m.IsVisible || m.Status != EnableStatus.Enabled)
            .ExecuteCommandAsync();

        if (addList.Count == 0 && showCount == 0)
        {
            Logger.LogInformation("系统菜单数据已存在且已显示，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化 {AddCount} 个系统菜单，显示 {ShowCount} 个代码生成菜单", addList.Count, showCount);
    }
}
