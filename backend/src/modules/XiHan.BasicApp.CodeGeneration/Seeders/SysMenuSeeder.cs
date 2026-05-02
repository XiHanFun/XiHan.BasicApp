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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Entities;
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
            addList.Add(new SysMenu { ParentId = null, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", IsExternal = false, IsCache = false, IsVisible = false, IsAffix = false, Status = EnableStatus.Disabled, Sort = 400, Remark = "代码生成前端和应用服务重建前暂不显示" });
        }

        if (!existsCodes.Contains("code_gen"))
        {
            addList.Add(new SysMenu { ParentId = null, MenuName = "代码生成", MenuCode = "code_gen", MenuType = MenuType.Menu, Path = "/develop/codeGen", Component = "Develop/CodeGen/Index", RouteName = "DevelopCodeGen", Icon = "lucide:code-xml", Title = "代码生成", IsExternal = false, IsCache = true, IsVisible = false, IsAffix = false, Status = EnableStatus.Disabled, Sort = 401, Remark = "代码生成前端和应用服务重建前暂不显示" });
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

        var hideCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.IsVisible == false)
            .SetColumns(m => m.Status == EnableStatus.Disabled)
            .SetColumns(m => m.Remark == "代码生成前端和应用服务重建前暂不显示")
            .Where(m => m.MenuCode == "develop" || m.MenuCode == "code_gen")
            .Where(m => m.IsVisible || m.Status != EnableStatus.Disabled)
            .ExecuteCommandAsync();

        if (addList.Count == 0 && hideCount == 0)
        {
            Logger.LogInformation("系统菜单数据已存在且保持隐藏，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化 {AddCount} 个系统菜单，隐藏 {HideCount} 个未完成菜单", addList.Count, hideCount);
    }
}
