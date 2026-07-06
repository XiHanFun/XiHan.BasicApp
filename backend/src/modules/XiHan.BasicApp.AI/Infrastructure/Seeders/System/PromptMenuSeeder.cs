#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PromptMenuSeeder
// Guid:0ddaafaf-8e7b-44c1-9dae-715b46be581b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 提示词库菜单种子数据
/// </summary>
/// <remarks>挂既有 develop 目录，建即绑 ai_prompt:read；缺失才兜底建 develop，自愈仅针对 ai_prompt 菜单。</remarks>
public class PromptMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PromptMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<PromptMenuSeeder> logger, IServiceProvider serviceProvider) : base(clientResolver, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级（置于提示词库权限种子之后）
    /// </summary>
    public override int Order => 211;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]提示词库菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        var readPermission = await client.Queryable<SysPermission>().FirstAsync(p => p.PermissionCode == "ai_prompt:read");
        if (readPermission is null)
        {
            Logger.LogWarning("ai_prompt:read 权限不存在，无法绑定提示词库菜单可见性，跳过提示词库菜单种子");
            return;
        }
        var readPermissionId = readPermission.BasicId;

        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "ai_prompt").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", I18nKey = "menu.develop", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 801, Remark = "开发工具目录" });
        }

        if (!existsCodes.Contains("ai_prompt"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "AI 提示词", MenuCode = "ai_prompt", MenuType = MenuType.Menu, Path = "/develop/aiPrompt", Component = "Develop/AiPrompt/Index", RouteName = "DevelopAiPrompt", Icon = "lucide:file-text", Title = "AI 提示词", I18nKey = "menu.ai_prompt", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 804, Remark = "AI 提示词库" });
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
                .Where(m => m.MenuCode == "ai_prompt")
                .ExecuteCommandAsync();
        }

        var fixedCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.PermissionId == readPermissionId)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "ai_prompt")
            .ExecuteCommandAsync();

        Logger.LogInformation("初始化 AI 提示词菜单：新增 {AddCount} 个，绑定权限/解除隐藏 {FixedCount} 个", addList.Count, fixedCount);
    }
}
