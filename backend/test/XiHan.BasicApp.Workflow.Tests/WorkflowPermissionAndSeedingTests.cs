// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.BasicApp.Workflow.Application.EventHandlers;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.BasicApp.Workflow.Extensions;
using XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.EventBus.Local;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Stores;
using SaasPageRegistry = XiHan.BasicApp.Saas.Application.Pages.PageRegistry;
using WorkflowPageRegistry = XiHan.BasicApp.Workflow.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流权限常量、页面登记表、种子链与模块服务登记的一致性测试。
/// </summary>
/// <remarks>
/// 权限码 / 资源码 / 操作码分散在三处（常量、资源种子、操作种子），任何一处漂移都不会报错，
/// 只会在干净库上表现为"菜单少了几条、接口一律 403"，且日志里只有一条 WRN。
/// 页面登记表同理：父目录码引用的是 Saas 模块的工作台目录，对方改码后"我的待办"会静默消失。
/// 本文件把这些跨文件、跨模块的隐式契约变成会红的断言。
/// </remarks>
public sealed class WorkflowPermissionAndSeedingTests
{
    /// <summary>
    /// 本模块权限链上的五个操作码（与操作种子、权限种子的目标集合同一份口径）。
    /// </summary>
    private static readonly string[] ExpectedOperationCodes = ["read", "create", "update", "delete", "execute"];

    /// <summary>
    /// 五个权限码必须互不重复，且全部以 <c>workflow:</c> 前缀开头。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldBeUniqueAndPrefixedByResource()
    {
        string[] codes =
        [
            WorkflowPermissionCodes.Read,
            WorkflowPermissionCodes.Create,
            WorkflowPermissionCodes.Update,
            WorkflowPermissionCodes.Delete,
            WorkflowPermissionCodes.Execute
        ];

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.StartsWith($"{WorkflowPermissionCodes.Resource}:", code, StringComparison.Ordinal));
        Assert.All(codes, code => Assert.Equal(code, code.ToLowerInvariant(), StringComparer.Ordinal));
    }

    /// <summary>
    /// 模块码与资源码必须相等且为 <c>workflow</c>：权限码由「资源码 : 操作码」派生，二者错开即整条链断掉。
    /// </summary>
    [Fact]
    public void ModuleAndResourceCode_ShouldBothBeWorkflow()
    {
        Assert.Equal("workflow", WorkflowPermissionCodes.Module, StringComparer.Ordinal);
        Assert.Equal("workflow", WorkflowPermissionCodes.Resource, StringComparer.Ordinal);
    }

    /// <summary>
    /// 权限码的操作后缀必须与操作种子内置的操作字典逐一对应，三处口径不一致会直接导致鉴权 403。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldMatchOperationSeederActions()
    {
        var seededOperationCodes = ReadBuiltInOperationCodes();

        Assert.Equal(
            ExpectedOperationCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray(),
            seededOperationCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray());

        string[] permissionCodes =
        [
            WorkflowPermissionCodes.Read,
            WorkflowPermissionCodes.Create,
            WorkflowPermissionCodes.Update,
            WorkflowPermissionCodes.Delete,
            WorkflowPermissionCodes.Execute
        ];
        var suffixes = permissionCodes
            .Select(code => code[(WorkflowPermissionCodes.Resource.Length + 1)..])
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ExpectedOperationCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray(),
            suffixes);
    }

    /// <summary>
    /// 页面码、路由路径、路由名与排序值都必须互不重复，重复会让菜单树出现覆盖或错位。
    /// </summary>
    [Fact]
    public void PageRegistry_KeysShouldBeUnique()
    {
        var pages = WorkflowPageRegistry.All;

        Assert.Equal(pages.Count, pages.Select(page => page.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(pages.Count, pages.Select(page => page.Path).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(pages.Count, pages.Select(page => page.RouteName).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(pages.Count, pages.Select(page => page.Sort).Distinct().Count());
    }

    /// <summary>
    /// 父目录必须排在子项之前：菜单种子按顺序解析 ParentId，倒序登记会让子菜单解析不到父节点被跳过。
    /// </summary>
    [Fact]
    public void PageRegistry_ParentsShouldPrecedeChildren()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var page in WorkflowPageRegistry.All)
        {
            if (page.ParentCode is { } parentCode
                && !seen.Contains(parentCode)
                && !string.Equals(parentCode, WorkflowPageRegistry.WorkbenchDirectoryCode, StringComparison.Ordinal))
            {
                violations.Add($"{page.Code} 的父级 {parentCode} 未在其之前登记");
            }

            _ = seen.Add(page.Code);
        }

        Assert.True(
            violations.Count == 0,
            $"页面登记顺序不满足「父先于子」：{string.Join("；", violations)}。");
    }

    /// <summary>
    /// 跨模块父目录码必须真实存在于 Saas 页面登记表中，否则「我的待办」会因解析不到父节点而静默消失。
    /// </summary>
    [Fact]
    public void PageRegistry_WorkbenchParentCode_ShouldExistInSaasRegistry()
    {
        Assert.Equal("workbench", WorkflowPageRegistry.WorkbenchDirectoryCode, StringComparer.Ordinal);
        Assert.Contains(
            SaasPageRegistry.All,
            page => string.Equals(page.Code, WorkflowPageRegistry.WorkbenchDirectoryCode, StringComparison.Ordinal));
    }

    /// <summary>
    /// 工作流目录由本模块独占登记：Saas 侧不得出现同码目录，否则两个模块的种子会互相覆盖父节点。
    /// </summary>
    [Fact]
    public void PageRegistry_WorkflowDirectory_ShouldBeOwnedExclusivelyByThisModule()
    {
        Assert.Equal("workflow", WorkflowPageRegistry.WorkflowDirectoryCode, StringComparer.Ordinal);
        Assert.DoesNotContain(
            SaasPageRegistry.All,
            page => string.Equals(page.Code, WorkflowPageRegistry.WorkflowDirectoryCode, StringComparison.Ordinal));
    }

    /// <summary>
    /// 目录项不得有组件路径、菜单项必须有组件路径，前端动态路由据此决定是否挂载页面组件。
    /// </summary>
    [Fact]
    public void PageRegistry_ComponentShouldMatchMenuType()
    {
        var violations = WorkflowPageRegistry.All
            .Where(page => page.MenuType == MenuType.Directory
                ? page.Component is not null
                : string.IsNullOrWhiteSpace(page.Component))
            .Select(page => $"{page.Code}({page.MenuType})")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列页面的组件路径与菜单类型不匹配（目录须为 null、菜单须非空）：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 国际化键必须是 <c>menu.{页面码把 . 与 - 换成 _}</c>，前端 menu.ts 按此约定维护双语文案。
    /// </summary>
    [Fact]
    public void PageRegistry_I18nKeyShouldFollowNamingConvention()
    {
        var violations = WorkflowPageRegistry.All
            .Where(page => !string.Equals(
                page.I18nKey,
                $"menu.{page.Code.Replace('.', '_').Replace('-', '_')}",
                StringComparison.Ordinal))
            .Select(page => $"{page.Code} → {page.I18nKey}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列页面的国际化键不符合 menu.{{code}} 约定：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 权限绑定必须精确：目录与「我的待办」不绑权限（登录即可见），流程定义 / 流程实例绑 workflow:read。
    /// </summary>
    [Fact]
    public void PageRegistry_PermissionBindingShouldStayExact()
    {
        var bindings = WorkflowPageRegistry.All.ToDictionary(page => page.Code, page => page.PermissionCode, StringComparer.Ordinal);

        Assert.Null(bindings[WorkflowPageRegistry.WorkflowDirectoryCode]);
        Assert.Null(bindings["workflow_todo"]);
        Assert.Equal(WorkflowPermissionCodes.Read, bindings["workflow_definition"], StringComparer.Ordinal);
        Assert.Equal(WorkflowPermissionCodes.Read, bindings["workflow_instance"], StringComparer.Ordinal);
    }

    /// <summary>
    /// 页面上绑定的权限码必须存在于本模块权限常量中，绑到不存在的权限码会让菜单永远不可见。
    /// </summary>
    [Fact]
    public void PageRegistry_BoundPermissionCodesShouldBeDeclared()
    {
        var declared = new HashSet<string>(
            [
                WorkflowPermissionCodes.Read,
                WorkflowPermissionCodes.Create,
                WorkflowPermissionCodes.Update,
                WorkflowPermissionCodes.Delete,
                WorkflowPermissionCodes.Execute
            ],
            StringComparer.Ordinal);

        var unknown = WorkflowPageRegistry.All
            .Select(page => page.PermissionCode)
            .Where(code => code is not null && !declared.Contains(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unknown.Count == 0,
            $"下列页面权限码未在 WorkflowPermissionCodes 中声明：{string.Join("、", unknown)}。");
    }

    /// <summary>
    /// 每个页面都必须有图标，缺图标的菜单在前端会塌成一行空白。
    /// </summary>
    [Fact]
    public void PageRegistry_EveryPageShouldDeclareIcon()
    {
        var violations = WorkflowPageRegistry.All
            .Where(page => string.IsNullOrWhiteSpace(page.Icon))
            .Select(page => page.Code)
            .ToList();

        Assert.True(violations.Count == 0, $"下列页面缺少图标：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 本模块暂无按钮级权限，按钮登记表必须为空——一旦新增按钮，需同步补齐按钮权限码的结构约束。
    /// </summary>
    [Fact]
    public void PageRegistry_ButtonsShouldStayEmpty()
    {
        Assert.Empty(WorkflowPageRegistry.Buttons);
    }

    /// <summary>
    /// 菜单种子必须以页面登记表为唯一事实源，直接内联菜单定义会让登记表与实际菜单分叉。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldSeedFromPageRegistry()
    {
        var seeder = CreateMenuSeeder();
        var pages = (IReadOnlyList<PageDescriptor>)typeof(WorkflowMenuSeeder)
            .GetProperty("Pages", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(seeder)!;
        var buttons = (IReadOnlyList<ButtonDescriptor>)typeof(WorkflowMenuSeeder)
            .GetProperty("Buttons", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(seeder)!;

        Assert.Same(WorkflowPageRegistry.All, pages);
        Assert.Same(WorkflowPageRegistry.Buttons, buttons);
    }

    /// <summary>
    /// 五个种子的执行序号必须锁死：操作 → 资源 → 权限 → 菜单 → 角色授权，任何一步提前都会静默跳过。
    /// </summary>
    [Fact]
    public void Seeders_OrderShouldFollowDependencyChain()
    {
        Assert.Equal(300, CreateOperationSeeder().Order);
        Assert.Equal(301, CreateResourceSeeder().Order);
        Assert.Equal(302, CreatePermissionSeeder().Order);
        Assert.Equal(303, CreateMenuSeeder().Order);
        Assert.Equal(304, CreateRolePermissionSeeder().Order);
    }

    /// <summary>
    /// 五个种子的序号必须落在工作流独占的 300 段内且互不重复，避免与 Saas / 代码生成 / AI 段交叠。
    /// </summary>
    [Fact]
    public void Seeders_OrdersShouldStayInWorkflowBandAndBeUnique()
    {
        var orders = CreateAllSeeders().Select(seeder => seeder.Order).ToList();

        Assert.Equal(orders.Count, orders.Distinct().Count());
        Assert.All(orders, order => Assert.InRange(order, 300, 399));
    }

    /// <summary>
    /// 五个种子的名称必须以模块前缀开头，启动日志里才能一眼定位是哪个模块的种子在跑。
    /// </summary>
    [Fact]
    public void Seeders_NamesShouldCarryModulePrefix()
    {
        var violations = CreateAllSeeders()
            .Where(seeder => !seeder.Name.StartsWith("[Workflow]", StringComparison.Ordinal))
            .Select(seeder => seeder.Name)
            .ToList();

        Assert.True(violations.Count == 0, $"下列种子名称缺少 [Workflow] 前缀：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 五个种子都必须在平台租户上下文内播种：落到启动时的租户下，按 TenantId = 0 查找的消费方会静默查不到。
    /// </summary>
    [Fact]
    public void Seeders_ShouldSeedWithinPlatformTenantScope()
    {
        var violations = typeof(SysOperationSeeder).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IDataSeeder)))
            .Where(type => !type.IsAssignableTo(typeof(PlatformDataSeederBase)))
            .Where(type => !type.IsAssignableTo(typeof(PageRegistryMenuSeederBase)))
            .Select(type => type.FullName!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列种子不在平台租户上下文内播种：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 模块内的种子类型必须正好是登记的五个，新增种子却漏登记服务时在这里变红。
    /// </summary>
    [Fact]
    public void Seeders_DiscoveredTypesShouldMatchRegisteredChain()
    {
        var discovered = typeof(SysOperationSeeder).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IDataSeeder)))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            new[]
            {
                nameof(SysOperationSeeder),
                nameof(SysPermissionSeeder),
                nameof(SysResourceSeeder),
                nameof(SysRolePermissionSeeder),
                nameof(WorkflowMenuSeeder)
            }.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            discovered.ToArray());
    }

    /// <summary>
    /// 存储替换必须用 Replace 而非 TryAdd：框架已 TryAddSingleton 内存默认实现，TryAdd 会被静默忽略。
    /// </summary>
    [Fact]
    public void AddWorkflowStores_ShouldReplaceFrameworkDefaultsWithSqlSugarSingletons()
    {
        var services = new ServiceCollection();
        services.TryAddSingletonPlaceholders();

        _ = services.AddWorkflowStores();

        AssertSingleReplacedStore<IWorkflowDefinitionStore, SqlSugarWorkflowDefinitionStore>(services);
        AssertSingleReplacedStore<IWorkflowInstanceStore, SqlSugarWorkflowInstanceStore>(services);
        AssertSingleReplacedStore<IWorkflowBookmarkStore, SqlSugarWorkflowBookmarkStore>(services);
    }

    /// <summary>
    /// 种子登记必须把五个种子全部登记成 <see cref="IDataSeeder"/>，漏一个则整条权限链在干净库上断掉。
    /// </summary>
    [Fact]
    public void AddWorkflowDataSeeders_ShouldRegisterEverySeeder()
    {
        var services = new ServiceCollection();

        _ = services.AddWorkflowDataSeeders();

        var registered = services
            .Where(descriptor => descriptor.ServiceType == typeof(IDataSeeder))
            .Select(descriptor => descriptor.ImplementationType!.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(SysOperationSeeder),
                nameof(SysPermissionSeeder),
                nameof(SysResourceSeeder),
                nameof(SysRolePermissionSeeder),
                nameof(WorkflowMenuSeeder)
            }.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            registered);
    }

    /// <summary>
    /// 事件处理器必须同时 AddTransient 并加入本地事件总线处理器列表——只 AddTransient 不会被订阅。
    /// </summary>
    [Fact]
    public void AddWorkflowEventHandlers_ShouldRegisterAndSubscribeEveryHandler()
    {
        var services = new ServiceCollection();

        _ = services.AddWorkflowEventHandlers();

        Type[] handlerTypes =
        [
            typeof(WorkflowUserTaskCreatedNotificationHandler),
            typeof(WorkflowUserTaskTransferredNotificationHandler),
            typeof(WorkflowInstanceFaultedNotificationHandler)
        ];
        var notRegistered = handlerTypes
            .Where(type => !services.Any(descriptor =>
                descriptor.ServiceType == type && descriptor.Lifetime == ServiceLifetime.Transient))
            .Select(type => type.Name)
            .ToList();
        Assert.True(notRegistered.Count == 0, $"下列处理器未 AddTransient：{string.Join("、", notRegistered)}。");

        using var provider = services.BuildServiceProvider();
        var handlers = provider.GetRequiredService<IOptions<XiHanLocalEventBusOptions>>().Value.Handlers;
        var notSubscribed = handlerTypes.Where(type => !handlers.Contains(type)).Select(type => type.Name).ToList();

        Assert.True(
            notSubscribed.Count == 0,
            $"下列处理器未加入 XiHanLocalEventBusOptions.Handlers，事件不会被投递：{string.Join("、", notSubscribed)}。");
    }

    /// <summary>
    /// 重复登记事件处理器必须幂等：模块被多次配置时处理器列表不得出现重复订阅（同一事件投递两遍通知）。
    /// </summary>
    [Fact]
    public void AddWorkflowEventHandlers_CalledTwice_ShouldStayIdempotent()
    {
        var services = new ServiceCollection();

        _ = services.AddWorkflowEventHandlers();
        _ = services.AddWorkflowEventHandlers();

        using var provider = services.BuildServiceProvider();
        var handlers = provider.GetRequiredService<IOptions<XiHanLocalEventBusOptions>>().Value.Handlers;

        Assert.Equal(3, handlers.Count);
    }

    /// <summary>
    /// 读取操作种子内置操作字典里的操作编码（私有静态字段，锁定"三处一致"这条跨文件约定）。
    /// </summary>
    /// <returns>操作编码集合。</returns>
    private static List<string> ReadBuiltInOperationCodes()
    {
        var field = typeof(SysOperationSeeder)
            .GetField("BuiltInOperations", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.True(field is not null, "SysOperationSeeder.BuiltInOperations 已改名，操作字典与权限码的一致性检查失效。");

        var codes = new List<string>();
        foreach (var operation in (System.Collections.IEnumerable)field!.GetValue(null)!)
        {
            var codeField = operation.GetType().GetField("Item1")!;
            codes.Add((string)codeField.GetValue(operation)!);
        }

        return codes;
    }

    /// <summary>
    /// 断言某个存储接口在服务集合里只剩一条登记且指向 SqlSugar 实现。
    /// </summary>
    /// <typeparam name="TService">存储接口。</typeparam>
    /// <typeparam name="TImplementation">期望的实现类型。</typeparam>
    /// <param name="services">服务集合。</param>
    private static void AssertSingleReplacedStore<TService, TImplementation>(IServiceCollection services)
    {
        var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(TService)).ToList();

        var descriptor = Assert.Single(descriptors);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    /// 构造模块内全部种子实例。
    /// </summary>
    /// <returns>种子实例集合。</returns>
    private static List<IDataSeeder> CreateAllSeeders()
    {
        return
        [
            CreateOperationSeeder(),
            CreateResourceSeeder(),
            CreatePermissionSeeder(),
            CreateMenuSeeder(),
            CreateRolePermissionSeeder()
        ];
    }

    /// <summary>
    /// 构造操作种子。
    /// </summary>
    /// <returns>操作种子。</returns>
    private static SysOperationSeeder CreateOperationSeeder()
    {
        return new SysOperationSeeder(
            Mock.Of<ISqlSugarClientResolver>(),
            new RecordingLogger<SysOperationSeeder>(),
            Mock.Of<IServiceProvider>());
    }

    /// <summary>
    /// 构造资源种子。
    /// </summary>
    /// <returns>资源种子。</returns>
    private static SysResourceSeeder CreateResourceSeeder()
    {
        return new SysResourceSeeder(
            Mock.Of<ISqlSugarClientResolver>(),
            new RecordingLogger<SysResourceSeeder>(),
            Mock.Of<IServiceProvider>());
    }

    /// <summary>
    /// 构造权限种子。
    /// </summary>
    /// <returns>权限种子。</returns>
    private static SysPermissionSeeder CreatePermissionSeeder()
    {
        return new SysPermissionSeeder(
            Mock.Of<ISqlSugarClientResolver>(),
            new RecordingLogger<SysPermissionSeeder>(),
            Mock.Of<IServiceProvider>());
    }

    /// <summary>
    /// 构造角色权限种子。
    /// </summary>
    /// <returns>角色权限种子。</returns>
    private static SysRolePermissionSeeder CreateRolePermissionSeeder()
    {
        return new SysRolePermissionSeeder(
            Mock.Of<ISqlSugarClientResolver>(),
            new RecordingLogger<SysRolePermissionSeeder>(),
            Mock.Of<IServiceProvider>());
    }

    /// <summary>
    /// 构造菜单种子。
    /// </summary>
    /// <returns>菜单种子。</returns>
    private static WorkflowMenuSeeder CreateMenuSeeder()
    {
        return new WorkflowMenuSeeder(
            Mock.Of<ISqlSugarClientResolver>(),
            new RecordingLogger<WorkflowMenuSeeder>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ICurrentTenant>());
    }
}

/// <summary>
/// 存储替换测试的辅助扩展。
/// </summary>
internal static class WorkflowStoreRegistrationTestExtensions
{
    /// <summary>
    /// 模拟框架 AddXiHanWorkflow 已用 TryAddSingleton 登记的内存默认实现。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection TryAddSingletonPlaceholders(this IServiceCollection services)
    {
        services.AddSingleton(Mock.Of<IWorkflowDefinitionStore>());
        services.AddSingleton(Mock.Of<IWorkflowInstanceStore>());
        services.AddSingleton(Mock.Of<IWorkflowBookmarkStore>());
        return services;
    }
}
