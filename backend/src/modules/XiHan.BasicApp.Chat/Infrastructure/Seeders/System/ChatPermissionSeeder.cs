// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders.System;

/// <summary>
/// 聊天模块权限种子数据
/// </summary>
/// <remarks>
/// 聊天权限为定义式功能权限（ResourceId/OperationId 为空，与 Saas 权限同构），
/// 须先于 <see cref="ChatMenuSeeder"/> 执行：菜单建立时即可解析 chat:read 绑定可见性。
/// </remarks>
public class ChatPermissionSeeder : PlatformDataSeederBase
{
    /// <summary>
    /// 权限定义（码、名称、描述、是否审计、排序；Priority 恒等于 Sort）
    /// </summary>
    private static readonly (string Code, string Name, string Description, bool Audit, int Sort)[] Definitions =
    [
        (ChatPermissionCodes.Read, "聊天查看", "查看当前用户的聊天会话列表与消息历史", true, 916),
        (ChatPermissionCodes.Send, "聊天发送", "在所属会话内发送消息与撤回自己的消息", true, 2078),
        (ChatPermissionCodes.Manage, "聊天会话管理", "创建群聊、添加/移除群成员", true, 2079),
        (ChatPermissionCodes.Audit, "聊天审计", "管理侧跨会话查询聊天消息（合规审计）", false, 2080)
    ];

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatPermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<ChatPermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（聊天种子统一在 Order 400+ 独立段，与 Saas/代码生成/AI/工作流不交叠）
    /// </summary>
    public override int Order => 400;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]系统权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var codes = Definitions.Select(d => d.Code).ToList();
        var existingCodes = (await client.Queryable<SysPermission>()
                .Where(p => p.TenantId == 0 && codes.Contains(p.PermissionCode))
                .ToListAsync())
            .Select(p => p.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addList = Definitions
            .Where(d => !existingCodes.Contains(d.Code))
            .Select(d => new SysPermission
            {
                TenantId = 0,
                PermissionType = PermissionType.Functional,
                ModuleCode = ChatPermissionCodes.Module,
                PermissionCode = d.Code,
                PermissionName = d.Name,
                PermissionDescription = d.Description,
                Tags = ChatPermissionCodes.Module,
                IsRequireAudit = d.Audit,
                Priority = d.Sort,
                Status = EnableStatus.Enabled,
                Sort = d.Sort,
                Remark = "系统初始化全局权限"
            })
            .ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("聊天权限数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个聊天权限", addList.Count);
    }
}
