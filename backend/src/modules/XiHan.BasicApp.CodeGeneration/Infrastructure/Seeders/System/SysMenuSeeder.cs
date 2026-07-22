// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders.System;

/// <summary>
/// 系统菜单种子数据
/// </summary>
/// <remarks>
/// 开发工具（develop 目录 + code_gen 菜单）属平台级开发工具，仅对拥有 code_gen:read 者（=超管通配 *）可见。
/// 故菜单建立即绑定 code_gen:read 权限：因权限种子（SysPermissionSeeder）先行，本种子可解析到该权限，
/// 在 INSERT 时即写入 PermissionId（新库直接到位，不依赖后置回填）；对既有库再幂等纠正一次（自愈）。
/// 非超管两者皆被 MenuRouteQueryService 按权限过滤掉（目录亦直接受控，不依赖空目录剪枝）。
/// </remarks>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider) : base(clientResolver, logger, serviceProvider) { }

    /// <summary>
    /// 种子数据优先级（置于 SysPermissionSeeder 之后，确保建菜单时 code_gen:read 已存在）
    /// </summary>
    public override int Order => 103;

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

        // 开发工具仅对 code_gen:read 拥有者可见。权限种子先行，此处解析其 Id，建菜单即绑定。
        var readPermission = await client.Queryable<SysPermission>().FirstAsync(p => p.PermissionCode == "code_gen:read");
        if (readPermission is null)
        {
            Logger.LogWarning("code_gen:read 权限不存在，无法绑定开发工具菜单可见性，跳过开发工具菜单种子");
            return;
        }
        var readPermissionId = readPermission.BasicId;

        var exists = await client.Queryable<SysMenu>().Where(m => m.MenuCode == "develop" || m.MenuCode == "code_gen").ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        if (!existsCodes.Contains("develop"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "开发工具", MenuCode = "develop", MenuType = MenuType.Directory, Path = "/develop", Component = null, RouteName = null, Icon = "lucide:hammer", Title = "开发工具", I18nKey = "menu.develop", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 801, Remark = "开发工具目录" });
        }

        if (!existsCodes.Contains("code_gen"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "代码生成", MenuCode = "code_gen", MenuType = MenuType.Menu, Path = "/develop/codeGen", Component = "Develop/CodeGen/Index", RouteName = "DevelopCodeGen", Icon = "lucide:code-xml", Title = "代码生成", I18nKey = "menu.code_gen", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 801, Remark = "代码生成" });
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

        // 幂等自愈：对既有库（含历史上 PermissionId 为空而泄漏给所有人的数据）统一绑定 code_gen:read、解除隐藏并启用。
        // 无条件回填，重复设同值无副作用；新增行已在 INSERT 时到位，此处保证既有行同样收敛。
        var fixedCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.PermissionId == readPermissionId)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "develop" || m.MenuCode == "code_gen")
            .ExecuteCommandAsync();

        Logger.LogInformation("初始化开发工具菜单：新增 {AddCount} 个，绑定权限/解除隐藏 {FixedCount} 个", addList.Count, fixedCount);
    }
}
