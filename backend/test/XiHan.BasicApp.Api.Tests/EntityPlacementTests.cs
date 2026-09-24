// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Routing;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 库隔离下的实体落点守卫。
/// </summary>
/// <remarks>
/// 平台库是目录库：平台目录、账号与成员关系、授权绑定、会话令牌、读共享模板与平台专属数据固定在这里（<see cref="PlatformDataSourceAttribute"/>）；
/// 库隔离租户的独立库只放租户业务与运行数据。两条约束决定落点：
/// <list type="bullet">
/// <item>读共享要让租户看见 TenantId=0 的平台行，平台行只在平台库——所以留在租户库的多租户实体只能是严格隔离的。</item>
/// <item>登录、令牌兑换、会话闸门在拿到租户之前就要定位行；平台还要跨租户维护账号与授权——这些实体必须集中在一个库。</item>
/// </list>
/// </remarks>
public sealed class EntityPlacementTests
{
    /// <summary>
    /// 承载业务实体的模块程序集
    /// </summary>
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(BasicApp.Saas.XiHanBasicAppSaasModule).Assembly,
        typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly,
        typeof(BasicApp.Chat.XiHanBasicAppChatModule).Assembly,
        typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly,
        typeof(BasicApp.Printing.XiHanBasicAppPrintingModule).Assembly,
        typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly
    ];

    /// <summary>
    /// 跨租户定位或维护的实体：租户目录、账号域、成员关系、授权绑定、会话与令牌
    /// </summary>
    private static readonly Type[] CrossTenantEntities =
    [
        typeof(SysTenant),
        typeof(SysTenantEdition),
        typeof(SysTenantEditionPermission),
        typeof(SysTenantUser),
        typeof(SysUser),
        typeof(SysUserSecurity),
        typeof(SysUserSetting),
        typeof(SysUserNotificationPreference),
        typeof(SysUserApiCredential),
        typeof(SysExternalLogin),
        typeof(SysPasswordHistory),
        typeof(SysUserSession),
        typeof(SysSessionRole),
        typeof(SysOAuthApp),
        typeof(SysOAuthCode),
        typeof(SysOAuthToken),
        typeof(SysUserRole),
        typeof(SysUserPermission),
        typeof(SysUserDataScope),
        typeof(SysPermissionDelegation),
        typeof(SysRole),
        typeof(SysRolePermission),
        typeof(SysRoleHierarchy),
        typeof(SysRoleDataScope),
        typeof(SysPermission),
        typeof(SysResource),
        typeof(SysOperation),
        typeof(SysMenu)
    ];

    /// <summary>
    /// 留在租户库的多租户实体必须严格隔离：读共享实体要看见平台行，只能放平台库。
    /// </summary>
    [Fact]
    public void TenantDatabaseEntities_ShouldBeStrictlyIsolated()
    {
        var violations = EnumerateEntities()
            .Where(type => type.IsAssignableTo(typeof(IMultiTenantEntity)))
            .Where(type => !IsPlatformPlaced(type))
            .Where(type => !type.IsAssignableTo(typeof(IStrictMultiTenantEntity)))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个实体留在租户库却是读共享：库隔离租户的库里没有平台行，" +
            $"字段隔离与库隔离的租户会看到不同的数据。要么严格隔离，要么固定在平台库：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 跨租户定位或维护的实体必须固定在平台库。
    /// </summary>
    [Fact]
    public void CrossTenantEntities_ShouldLiveInPlatformDatabase()
    {
        var violations = CrossTenantEntities
            .Where(type => !IsPlatformPlaced(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个实体要跨租户定位或维护，却不在平台库：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 平台库落点与模块数据源互斥：同时声明是配置错误。
    /// </summary>
    [Fact]
    public void PlatformPlacedEntities_ShouldNotDeclareModuleDataSource()
    {
        var violations = EnumerateEntities()
            .Where(IsPlatformPlaced)
            .Where(type => type.GetCustomAttribute<ModuleDataSourceAttribute>(inherit: true) is not null)
            .Select(type => type.Name)
            .ToList();

        Assert.Empty(violations);
    }

    private static IEnumerable<Type> EnumerateEntities()
    {
        return ModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetCustomAttributes<SugarTable>(inherit: false).Any());
    }

    private static bool IsPlatformPlaced(Type type)
    {
        return type.GetCustomAttribute<PlatformDataSourceAttribute>(inherit: true) is not null;
    }
}
