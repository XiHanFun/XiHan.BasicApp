#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuSeeder
// Guid:a11c0de0-3004-4a10-9a00-00000000ai33
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 系统菜单种子数据
/// </summary>
/// <remarks>
/// AI 提供商配置属平台级开发工具，挂在既有 develop 目录下，仅对拥有 ai:read 者（=超管通配 *）可见。
/// 权限种子（SysPermissionSeeder）先行，本种子解析 ai:read 并在 INSERT 时即写入 PermissionId（建即绑，不靠后置回填）。
/// develop 目录通常已由代码生成模块（Order 103）建立；本种子仅在其缺失时兜底创建，且只对 ai_provider 菜单自愈，
/// 不改动 develop 目录的既有权限绑定（避免与代码生成模块的自愈互相覆盖）。
/// </remarks>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider) : base(clientResolver, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级（置于 SysPermissionSeeder 之后，确保建菜单时 ai:read 已存在）
    /// </summary>
    public override int Order => 203;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        // AI 菜单仅对 ai:read 拥有者可见。权限种子先行，此处解析其 Id，建菜单即绑定。
        var readPermission = await client.Queryable<SysPermission>().FirstAsync(p => p.PermissionCode == "ai:read");
        if (readPermission is null)
        {
            Logger.LogWarning("ai:read 权限不存在，无法绑定 AI 菜单可见性，跳过 AI 菜单种子");
            return;
        }
        var readPermissionId = readPermission.BasicId;

        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "ai_provider").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        // develop 目录兜底（通常已由代码生成模块建立；缺失时以 ai:read 绑定创建）
        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", I18nKey = "menu.develop", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 801, Remark = "开发工具目录" });
        }

        if (!existsCodes.Contains("ai_provider"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "AI 提供商", MenuCode = "ai_provider", MenuType = MenuType.Menu, Path = "/develop/aiProvider", Component = "Develop/AiProvider/Index", RouteName = "DevelopAiProvider", Icon = "lucide:sparkles", Title = "AI 提供商", I18nKey = "menu.ai_provider", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 802, Remark = "AI 提供商配置" });
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
                .Where(m => m.MenuCode == "ai_provider")
                .ExecuteCommandAsync();
        }

        // 幂等自愈：仅对 ai_provider 菜单统一绑定 ai:read、解除隐藏并启用（不触碰 develop 目录的既有绑定）。
        var fixedCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.PermissionId == readPermissionId)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "ai_provider")
            .ExecuteCommandAsync();

        Logger.LogInformation("初始化 AI 菜单：新增 {AddCount} 个，绑定权限/解除隐藏 {FixedCount} 个", addList.Count, fixedCount);
    }
}
