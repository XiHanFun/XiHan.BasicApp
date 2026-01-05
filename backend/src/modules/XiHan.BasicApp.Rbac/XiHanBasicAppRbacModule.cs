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
using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.DataPermissions.Extensions;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Seeders;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Utils.Reflections;
using XiHan.Framework.Utils.Threading;
using XiHan.Framework.Web.Core.Extensions;

namespace XiHan.BasicApp.Rbac;

/// <summary>
/// 曦寒基础应用角色控制应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppCoreModule),
    typeof(XiHanBasicAppWebCoreModule)
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

        // 添加 RBAC 服务和仓储
        services.AddRbacRepositories();
        services.AddRbacServices();

        // 注册领域管理器
        services.AddScoped<UserManager>();
        services.AddScoped<RoleManager>();
        services.AddScoped<PermissionManager>();
        services.AddScoped<MenuManager>();
        services.AddScoped<DepartmentManager>();
        services.AddScoped<TenantManager>();

        // 添加数据权限支持
        services.AddDataPermission();

        // 配置SqlSugar选项
        var config = services.GetConfiguration().GetSection("XiHanSqlSugarCore");

        // 从配置文件绑定基础配置
        Configure<XiHanSqlSugarCoreOptions>(config);

        // 配置实体类型和其他选项
        Configure<XiHanSqlSugarCoreOptions>(options =>
        {
            // 获取所有带 SugarTable 特性的实体类型
            var dbEntities = ReflectionHelper.GetContainsAttributeSubClasses<IEntityBase, SugarTable>().ToList();
            options.EntityTypes = dbEntities;

            // 可以在这里添加其他配置
            // 例如：启用数据库初始化和种子数据（如果配置文件中未设置）
            // options.EnableDbInitialization = true;
            // options.EnableDataSeeding = true;
        });

        // 注册种子数据提供者
        services.AddDataSeeder<SysRoleSeeder>();
        services.AddDataSeeder<SysUserSeeder>();
        services.AddDataSeeder<SysUserRoleSeeder>();
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        AsyncHelper.RunSync(async () =>
        {
            await app.UseDbInitializerAsync(initialize: true);
        });
    }
}
