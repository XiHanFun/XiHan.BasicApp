#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeMenuSeeder
// Guid:8263780c-6757-4cb0-bfb1-6c7c15a72cca
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 知识库菜单种子数据
/// </summary>
/// <remarks>
/// 知识库属平台级开发工具，挂既有 develop 目录，仅对 knowledge_base:read 拥有者（=超管）可见。
/// 权限种子先行，建菜单即在 INSERT 绑 PermissionId；develop 目录通常已由代码生成/AI 模块建立，缺失时兜底创建，
/// 自愈仅针对 knowledge_base 菜单，不触碰 develop 目录既有绑定。
/// </remarks>
public class KnowledgeMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<KnowledgeMenuSeeder> logger, IServiceProvider serviceProvider) : base(clientResolver, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级（置于知识库权限种子之后）
    /// </summary>
    public override int Order => 207;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]知识库菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        var readPermission = await client.Queryable<SysPermission>().FirstAsync(p => p.PermissionCode == "knowledge_base:read");
        if (readPermission is null)
        {
            Logger.LogWarning("knowledge_base:read 权限不存在，无法绑定知识库菜单可见性，跳过知识库菜单种子");
            return;
        }
        var readPermissionId = readPermission.BasicId;

        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "knowledge_base").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", I18nKey = "menu.develop", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 801, Remark = "开发工具目录" });
        }

        if (!existsCodes.Contains("knowledge_base"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "知识库", MenuCode = "knowledge_base", MenuType = MenuType.Menu, Path = "/develop/knowledge", Component = "Develop/Knowledge/Index", RouteName = "DevelopKnowledge", Icon = "lucide:book-open", Title = "知识库", I18nKey = "menu.knowledge_base", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 803, Remark = "RAG 知识库" });
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
                .Where(m => m.MenuCode == "knowledge_base")
                .ExecuteCommandAsync();
        }

        var fixedCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.PermissionId == readPermissionId)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "knowledge_base")
            .ExecuteCommandAsync();

        Logger.LogInformation("初始化知识库菜单：新增 {AddCount} 个，绑定权限/解除隐藏 {FixedCount} 个", addList.Count, fixedCount);
    }
}
