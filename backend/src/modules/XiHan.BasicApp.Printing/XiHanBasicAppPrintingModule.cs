// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.BasicApp.Printing.Extensions;
using XiHan.BasicApp.Saas;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 曦寒基础应用打印应用模块
/// </summary>
/// <remarks>
/// 独立一等模块（与 AI/代码生成/工作流同构）：打印模板的租户/平台双作用域管理、
/// 按编码解析与分布式缓存，消费方为业务模块的单据打印与前端打印设计器。
/// 依赖 Saas（复用 RBAC 表、SaasRepository、菜单/权限种子基类）。
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppPrintingModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 全局模板开放管理属平台专属：登记进 Saas 的平台专属权限清单（版本白名单与租户授权双向排除）
        SaasPlatformPermissions.ContributePlatformOnly(PrintingPermissionCodes.GlobalManage);

        // 种子（操作 → 资源 → 权限 → 菜单 → 角色授权）+ 领域服务 + 解析器与缓存
        services.AddPrintingDataSeeders();
        services.AddPrintingDomainServices();
        services.AddPrintingApplicationServices();
    }
}
