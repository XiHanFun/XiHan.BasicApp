// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Printing.Infrastructure.Seeders.System;

/// <summary>
/// 打印模块权限种子数据
/// </summary>
/// <remarks>
/// 打印权限为定义式功能权限（ResourceId/OperationId 为空，与 Saas 权限同构），
/// 须先于 <see cref="PrintingMenuSeeder"/> 执行：菜单建立时即可解析 print-template:read 绑定可见性。
/// </remarks>
public class PrintingPermissionSeeder : PlatformDataSeederBase
{
    /// <summary>
    /// 权限定义（码、名称、描述、是否审计、排序；Priority 恒等于 Sort）
    /// </summary>
    private static readonly (string Code, string Name, string Description, bool Audit, int Sort)[] Definitions =
    [
        (PrintingPermissionCodes.Read, "打印模板查看", "查看当前作用域打印模板列表与详情", false, 2800),
        (PrintingPermissionCodes.Create, "打印模板创建", "创建当前作用域打印模板", true, 2801),
        (PrintingPermissionCodes.Update, "打印模板编辑", "编辑打印模板元数据和 hiprint 设计 JSON", true, 2802),
        (PrintingPermissionCodes.Status, "打印模板启停", "启用或停用打印模板", true, 2803),
        (PrintingPermissionCodes.Delete, "打印模板删除", "删除已经停用的打印模板", true, 2804),
        (PrintingPermissionCodes.Use, "打印模板使用", "按编码解析模板并执行预览或直接打印", true, 2805),
        (PrintingPermissionCodes.GlobalManage, "全局打印模板管理", "管理平台全局打印模板及租户开放状态", true, 2806)
    ];

    /// <summary>
    /// 构造函数
    /// </summary>
    public PrintingPermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<PrintingPermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（打印种子统一在 Order 500+ 独立段，与 Saas/代码生成/AI/工作流不交叠）
    /// </summary>
    public override int Order => 500;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Printing]系统权限种子数据";

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
                ModuleCode = PrintingPermissionCodes.Module,
                PermissionCode = d.Code,
                PermissionName = d.Name,
                PermissionDescription = d.Description,
                Tags = PrintingPermissionCodes.Module,
                IsRequireAudit = d.Audit,
                Priority = d.Sort,
                Status = EnableStatus.Enabled,
                Sort = d.Sort,
                Remark = "系统初始化全局权限"
            })
            .ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("打印权限数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个打印权限", addList.Count);
    }
}
