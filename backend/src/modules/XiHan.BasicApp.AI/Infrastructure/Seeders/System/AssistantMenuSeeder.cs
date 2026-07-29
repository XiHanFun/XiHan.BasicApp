// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 助手菜单种子数据
/// </summary>
/// <remarks>挂既有 develop 目录，建即绑 ai_assistant:read；缺失才兜底建 develop，自愈仅针对 ai_assistant 菜单。</remarks>
public class AssistantMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AssistantMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<AssistantMenuSeeder> logger, IServiceProvider serviceProvider) : base(clientResolver, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级（置于助手权限种子之后）
    /// </summary>
    public override int Order => 215;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]助手菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        var readPermission = await client.Queryable<SysPermission>().FirstAsync(p => p.PermissionCode == "ai_assistant:read");
        if (readPermission is null)
        {
            Logger.LogWarning("ai_assistant:read 权限不存在，无法绑定助手菜单可见性，跳过助手菜单种子");
            return;
        }
        var readPermissionId = readPermission.BasicId;

        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "ai_assistant").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", I18nKey = "menu.develop", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 801, Remark = "开发工具目录" });
        }

        if (!existsCodes.Contains("ai_assistant"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "AI 助手", MenuCode = "ai_assistant", MenuType = MenuType.Menu, Path = "/develop/aiAssistant", Component = "Develop/AiAssistant/Index", RouteName = "DevelopAiAssistant", Icon = "lucide:bot", Title = "AI 助手", I18nKey = "menu.ai_assistant", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 805, Remark = "AI 助手配置" });
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
                .Where(m => m.MenuCode == "ai_assistant")
                .ExecuteCommandAsync();
        }

        var fixedCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.PermissionId == readPermissionId)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "ai_assistant")
            .ExecuteCommandAsync();

        Logger.LogInformation("初始化 AI 助手菜单：新增 {AddCount} 个，绑定权限/解除隐藏 {FixedCount} 个", addList.Count, fixedCount);
    }
}
