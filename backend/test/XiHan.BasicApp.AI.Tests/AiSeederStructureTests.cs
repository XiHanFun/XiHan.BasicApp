// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.BasicApp.AI.Infrastructure.Seeders;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using AiPageRegistry = XiHan.BasicApp.AI.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 模块种子器的结构约束测试（反射型，不连库）
/// </summary>
/// <remarks>
/// 模块只有两个种子：权限目录（模型服务、提示词、助手、知识库四个资源 × 操作）与菜单。
/// 权限目录必须先于菜单：菜单建立时按权限码绑定可见性，权限没落库时菜单种子直接报错。
/// 两个种子各在自己的阶段、用 AI 的号段（+20），与其它模块错开。
/// </remarks>
public sealed class AiSeederStructureTests
{
    /// <summary>
    /// 种子器清单：权限目录与菜单，模块不写角色授权。
    /// </summary>
    [Fact]
    public void ModuleAssembly_Seeders_ShouldMatchExpectedRoster()
    {
        var names = SeederTypes().Select(type => type.Name).ToList();

        Assert.Equal([nameof(AiMenuSeeder), nameof(AiPermissionCatalogSeeder)], names);
    }

    /// <summary>
    /// 两个种子都在平台上下文里播，名称带 [AI] 前缀。
    /// </summary>
    [Fact]
    public void Seeders_ShouldSeedWithinPlatformScopeWithModulePrefix()
    {
        Assert.All(SeederTypes(), type =>
        {
            Assert.True(type.IsAssignableTo(typeof(PlatformDataSeederBase)), $"{type.Name} 没有继承平台种子基类。");
            Assert.True(type.IsSealed, $"{type.Name} 不是 sealed。");
            Assert.StartsWith("[AI]", ReadProperty<string>(type, "Name"), StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// 权限目录在权限目录阶段、菜单在菜单阶段，都用 AI 的号段。
    /// </summary>
    [Fact]
    public void Seeders_ShouldRunInTheirPhases()
    {
        Assert.Equal(SeedOrders.PermissionCatalog + 20, ReadProperty<int>(typeof(AiPermissionCatalogSeeder), "Order"));
        Assert.Equal(SeedOrders.Menus + 20, ReadProperty<int>(typeof(AiMenuSeeder), "Order"));
    }

    /// <summary>
    /// 权限目录：四个资源，各自的权限码与常量表一致，全部是平台侧。
    /// </summary>
    [Fact]
    public void PermissionCatalog_ShouldDeclareFourPlatformResources()
    {
        var catalog = Catalog();

        Assert.Equal(AiPermissionCodes.Module, catalog.ModuleCode);
        Assert.Equal(
            [AiPermissionCodes.Resource, AiPromptPermissionCodes.Resource, AiAssistantPermissionCodes.Resource, KnowledgePermissionCodes.Resource],
            catalog.Resources.Select(resource => resource.Code));
        Assert.Equal(DeclaredCodes().Order(StringComparer.Ordinal), catalog.Permissions.Select(permission => permission.Code).Order(StringComparer.Ordinal));
        Assert.All(catalog.Permissions, permission =>
        {
            Assert.Equal(PermissionSide.Platform, permission.Side);
            Assert.Contains(permission.Resource!, catalog.Resources);
            Assert.Contains(permission.Operation!, OperationSeeds.All);
        });
        Assert.Equal(catalog.Permissions.Count, catalog.Permissions.Select(permission => permission.Sort).Distinct().Count());
        Assert.Equal(catalog.Resources.Count, catalog.Resources.Select(resource => resource.Sort).Distinct().Count());
    }

    /// <summary>
    /// 菜单种子直接复用页面登记表；页面与按钮绑定的权限都在权限目录里。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldReusePageRegistryAndBindCatalogPermissions()
    {
        var instance = RuntimeHelpers.GetUninitializedObject(typeof(AiMenuSeeder));
        Assert.True(typeof(AiMenuSeeder).IsAssignableTo(typeof(PageRegistryMenuSeederBase)));
        Assert.Same(AiPageRegistry.All, ReadProperty<IReadOnlyList<PageDescriptor>>(typeof(AiMenuSeeder), "Pages", instance));
        Assert.Same(AiPageRegistry.Buttons, ReadProperty<IReadOnlyList<ButtonDescriptor>>(typeof(AiMenuSeeder), "Buttons", instance));

        var catalogCodes = Catalog().Permissions.Select(permission => permission.Code).ToHashSet(StringComparer.Ordinal);
        var bound = AiPageRegistry.All.Select(page => page.PermissionCode).OfType<string>()
            .Concat(AiPageRegistry.Buttons.Select(button => button.PermissionCode));
        Assert.All(bound, code => Assert.Contains(code, catalogCodes));
    }

    private static IEnumerable<string> DeclaredCodes()
    {
        return new[] { typeof(AiPermissionCodes), typeof(AiPromptPermissionCodes), typeof(AiAssistantPermissionCodes), typeof(KnowledgePermissionCodes) }
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.Name is not ("Module" or "Resource"))
            .Select(field => (string)field.GetRawConstantValue()!);
    }

    private static AiPermissionCatalogSeeder Catalog()
    {
        return new AiPermissionCatalogSeeder(Mock.Of<ISqlSugarClientResolver>(), NullLogger<AiPermissionCatalogSeeder>.Instance, Mock.Of<IServiceProvider>());
    }

    private static List<Type> SeederTypes()
    {
        return typeof(AiMenuSeeder).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IDataSeeder)))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static T ReadProperty<T>(Type type, string name, object? instance = null)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{type.Name} 上未找到属性 {name}。");
        return (T)property.GetValue(instance ?? RuntimeHelpers.GetUninitializedObject(type))!;
    }
}
