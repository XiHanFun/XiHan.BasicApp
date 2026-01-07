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
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Seeders;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Authentication;
using XiHan.Framework.Authorization;
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
    typeof(XiHanBasicAppWebCoreModule),
    typeof(XiHanAuthenticationModule),
    typeof(XiHanAuthorizationModule)
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

        // 1. 注册基础设施（Repositories）
        services.AddRbacRepositories();

        // 2. 注册领域服务（Domain Layer）
        services.AddScoped<UserDomainService>();
        services.AddScoped<RoleDomainService>();
        services.AddScoped<PermissionDomainService>();
        services.AddScoped<MenuDomainService>();
        services.AddScoped<DepartmentDomainService>();
        services.AddScoped<TenantDomainService>();
        services.AddScoped<AuthorizationDomainService>();  // 新增：处理授权和数据权限过滤

        // 3. 注册应用服务（Application Layer）
        services.AddRbacServices();

        // 4. 注册基础设施适配器（Infrastructure Layer）
        RegisterInfrastructureAdapters(services);

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

        // 注册种子数据提供者（按执行顺序注册）
        services.AddDataSeeder<SysOperationSeeder>();        // Order = 1
        services.AddDataSeeder<SysResourceSeeder>();          // Order = 2
        services.AddDataSeeder<SysPermissionSeeder>();        // Order = 3
        services.AddDataSeeder<SysDepartmentSeeder>();        // Order = 4
        services.AddDataSeeder<SysRoleSeeder>();              // Order = 10
        services.AddDataSeeder<SysRolePermissionSeeder>();    // Order = 15
        services.AddDataSeeder<SysUserSeeder>();              // Order = 20
        services.AddDataSeeder<SysMenuSeeder>();              // Order = 25
        services.AddDataSeeder<SysUserRoleSeeder>();          // Order = 30
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

    /// <summary>
    /// 注册基础设施适配器
    /// </summary>
    /// <remarks>
    /// Adapters 属于基础设施层，作为防腐层（Anti-Corruption Layer）隔离框架接口和领域模型
    /// 所有适配器只做接口转换，不包含业务逻辑，完全委托给 Domain Services 和 Application Services
    /// </remarks>
    /// <param name="services"></param>
    private static void RegisterInfrastructureAdapters(IServiceCollection services)
    {
        // 暂时注释掉，稍后创建极简实现
        // 认证适配器
        // services.AddScoped<IAuthenticationService, RbacAuthenticationService>();

        // 授权适配器
        // services.AddScoped<IPermissionStore, RbacPermissionStore>();
        // services.AddScoped<IRoleStore, RbacRoleStore>();
        // services.AddScoped<IRoleManager, RbacRoleManager>();
        // services.AddSingleton<IPolicyStore, RbacPolicyStore>();
        // services.AddScoped<IPolicyEvaluator, RbacPolicyEvaluator>();
    }
}
