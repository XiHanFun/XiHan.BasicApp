#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppRbacModule
// Guid:9b39d543-6e3f-46b8-a288-40076def6e6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:24:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.DataPermissions.Extensions;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.Rbac;

/// <summary>
/// 曦寒基础应用角色控制应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppCoreModule)
)]
public class XiHanBasicAppRbacModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 注册领域管理器
        services.AddScoped<UserManager>();
        services.AddScoped<RoleManager>();
        services.AddScoped<PermissionManager>();
        services.AddScoped<MenuManager>();
        services.AddScoped<DepartmentManager>();
        services.AddScoped<TenantManager>();

        // 添加 RBAC 服务和仓储
        services.AddRbacServices();
        services.AddRbacRepositories();

        // 添加数据权限支持
        services.AddDataPermission();
    }
}
