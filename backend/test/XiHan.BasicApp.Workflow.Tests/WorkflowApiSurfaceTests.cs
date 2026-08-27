// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using XiHan.BasicApp.Workflow.Application;
using XiHan.BasicApp.Workflow.Application.AppServices;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.QueryServices;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流动态 API 暴露面的反射型结构约束测试。
/// </summary>
/// <remarks>
/// 应用服务被 <c>[DynamicApi]</c> 自动暴露成 HTTP 接口，因此"漏挂权限特性"不会有任何编译或启动报错，
/// 只会在生产上变成一个人人可调的匿名接口。本文件把每个方法的权限码逐一钉死，
/// 并把两条刻意的例外（待办接口登录即可、工作流写操作不挂工作单元）写成显式断言，
/// 以免后来者"顺手补齐"反而破坏契约。
/// </remarks>
public sealed class WorkflowApiSurfaceTests
{
    /// <summary>
    /// 方法 → 权限码的完整期望映射（键为「类型名.方法名」）。
    /// </summary>
    private static readonly Dictionary<string, string> ExpectedPermissionCodes = new(StringComparer.Ordinal)
    {
        ["WorkflowDefinitionAppService.CreateAsync"] = WorkflowPermissionCodes.Create,
        ["WorkflowDefinitionAppService.NewVersionAsync"] = WorkflowPermissionCodes.Create,
        ["WorkflowDefinitionAppService.UpdateDraftAsync"] = WorkflowPermissionCodes.Update,
        ["WorkflowDefinitionAppService.PublishAsync"] = WorkflowPermissionCodes.Update,
        ["WorkflowDefinitionAppService.DisableAsync"] = WorkflowPermissionCodes.Update,
        ["WorkflowDefinitionAppService.ArchiveAsync"] = WorkflowPermissionCodes.Update,
        ["WorkflowDefinitionAppService.DeleteAsync"] = WorkflowPermissionCodes.Delete,
        ["WorkflowDefinitionQueryService.GetPageAsync"] = WorkflowPermissionCodes.Read,
        ["WorkflowDefinitionQueryService.GetDetailAsync"] = WorkflowPermissionCodes.Read,
        ["WorkflowInstanceAppService.StartAsync"] = WorkflowPermissionCodes.Execute,
        ["WorkflowInstanceAppService.CancelAsync"] = WorkflowPermissionCodes.Execute,
        ["WorkflowInstanceAppService.TerminateAsync"] = WorkflowPermissionCodes.Execute,
        ["WorkflowInstanceAppService.RetryAsync"] = WorkflowPermissionCodes.Execute,
        ["WorkflowInstanceAppService.PublishSignalAsync"] = WorkflowPermissionCodes.Execute,
        ["WorkflowInstanceAppService.SuspendAsync"] = WorkflowPermissionCodes.Update,
        ["WorkflowInstanceAppService.ResumeAsync"] = WorkflowPermissionCodes.Update,
        ["WorkflowInstanceQueryService.GetPageAsync"] = WorkflowPermissionCodes.Read,
        ["WorkflowInstanceQueryService.GetDetailAsync"] = WorkflowPermissionCodes.Read
    };

    /// <summary>
    /// 待办服务的接口刻意不设权限码（登录即可办理，受理人归属由任务服务在实例锁内校验）。
    /// </summary>
    private static readonly Type[] PermissionFreeServices =
    [
        typeof(WorkflowTodoAppService),
        typeof(WorkflowTodoQueryService)
    ];

    /// <summary>
    /// 模块内全部动态 API 应用服务。
    /// </summary>
    public static TheoryData<Type> AllApplicationServices => [.. DiscoverApplicationServices()];

    /// <summary>
    /// 应用服务基类必须同时带 <c>[Authorize]</c> 与 <c>[DynamicApi]</c>：
    /// 前者保证匿名请求直接 401，后者决定接口分组。
    /// </summary>
    [Fact]
    public void ApplicationServiceBase_ShouldCarryAuthorizeAndDynamicApi()
    {
        var baseType = typeof(WorkflowApplicationService);

        Assert.True(
            baseType.GetCustomAttribute<AuthorizeAttribute>(inherit: false) is not null,
            "WorkflowApplicationService 丢了 [Authorize]，全部工作流接口会退化成匿名可调。");

        var dynamicApi = baseType.GetCustomAttribute<DynamicApiAttribute>(inherit: false);
        Assert.True(dynamicApi is not null, "WorkflowApplicationService 丢了 [DynamicApi]，接口不会被暴露。");
        Assert.Equal("BasicApp.Workflow", dynamicApi!.Group, StringComparer.Ordinal);
    }

    /// <summary>
    /// 模块内全部应用服务都必须继承工作流应用服务基类，否则拿不到类级鉴权与分组。
    /// </summary>
    [Fact]
    public void ApplicationServices_ShouldDeriveFromWorkflowApplicationService()
    {
        var violations = DiscoverApplicationServices()
            .Where(type => !type.IsAssignableTo(typeof(WorkflowApplicationService)))
            .Select(type => type.FullName!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列应用服务未继承 WorkflowApplicationService，将失去类级 [Authorize] 与 DynamicApi 分组：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 五个应用服务都必须显式登记到工作流分组，接口文档与前端 API 模块按分组生成。
    /// </summary>
    /// <param name="serviceType">被检查的应用服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServices))]
    public void ApplicationService_ShouldDeclareWorkflowDynamicApiGroup(Type serviceType)
    {
        var dynamicApi = serviceType.GetCustomAttribute<DynamicApiAttribute>(inherit: false);

        Assert.True(dynamicApi is not null, $"{serviceType.Name} 缺少 [DynamicApi]，接口分组会回落到基类分组。");
        Assert.Equal("BasicApp.Workflow", dynamicApi!.Group, StringComparer.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(dynamicApi.Tag), $"{serviceType.Name} 的 [DynamicApi] 未指定 Tag。");
    }

    /// <summary>
    /// 每个受保护接口的权限码必须与期望映射逐一吻合：漏挂、错挂、多挂都在这里变红。
    /// </summary>
    [Fact]
    public void ProtectedEndpoints_ShouldCarryExpectedPermissionCode()
    {
        var actual = DiscoverApplicationServices()
            .Where(type => !PermissionFreeServices.Contains(type))
            .SelectMany(type => DiscoverEndpoints(type).Select(method => (Type: type, Method: method)))
            .ToDictionary(
                pair => $"{pair.Type.Name}.{pair.Method.Name}",
                pair => pair.Method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: false)
                    .Select(attribute => attribute.PermissionCode)
                    .SingleOrDefault(),
                StringComparer.Ordinal);

        var missing = ExpectedPermissionCodes.Keys.Where(key => !actual.ContainsKey(key)).ToList();
        var unexpected = actual.Keys.Where(key => !ExpectedPermissionCodes.ContainsKey(key)).ToList();
        var mismatched = actual
            .Where(pair => ExpectedPermissionCodes.TryGetValue(pair.Key, out var expected)
                && !string.Equals(expected, pair.Value, StringComparison.Ordinal))
            .Select(pair => $"{pair.Key}：期望 {ExpectedPermissionCodes[pair.Key]}，实际 {pair.Value ?? "无权限特性"}")
            .ToList();

        Assert.True(
            missing.Count == 0 && unexpected.Count == 0 && mismatched.Count == 0,
            $"工作流动态 API 的权限码与期望不符。" +
            $"{Environment.NewLine}期望存在但未找到的接口：{string.Join("、", missing)}" +
            $"{Environment.NewLine}新增但未登记权限期望的接口：{string.Join("、", unexpected)}" +
            $"{Environment.NewLine}权限码不匹配：{string.Join("；", mismatched)}");
    }

    /// <summary>
    /// 权限特性里出现的每个权限码都必须来自本模块权限常量，杜绝手写字面量拼错。
    /// </summary>
    [Fact]
    public void PermissionAttributes_ShouldOnlyUseModulePermissionConstants()
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

        var unknown = DiscoverApplicationServices()
            .SelectMany(DiscoverEndpoints)
            .SelectMany(method => method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: false))
            .Select(attribute => attribute.PermissionCode)
            .Where(code => !declared.Contains(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unknown.Count == 0,
            $"下列权限码不在 WorkflowPermissionCodes 常量中，鉴权时必然 403：{string.Join("、", unknown)}。");
    }

    /// <summary>
    /// 待办接口刻意不挂权限码：登录即可办理自己的待办。补挂权限码会让未获授权的普通审批人无法签批。
    /// </summary>
    /// <param name="serviceType">待办服务类型。</param>
    [Theory]
    [InlineData(typeof(WorkflowTodoAppService))]
    [InlineData(typeof(WorkflowTodoQueryService))]
    public void TodoEndpoints_ShouldStayPermissionFree(Type serviceType)
    {
        var guarded = DiscoverEndpoints(serviceType)
            .Where(method => method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: false).Any())
            .Select(method => $"{serviceType.Name}.{method.Name}")
            .ToList();

        Assert.True(
            guarded.Count == 0,
            $"待办接口被挂上了权限码：{string.Join("、", guarded)}。" +
            $"待办办理只依赖登录身份与受理人归属校验，挂权限码会让未授权的普通审批人无法签批。");
    }

    /// <summary>
    /// 工作流写操作刻意不挂 <c>[UnitOfWork]</c>：引擎存储按操作独立作用域持久化，与请求事务无关。
    /// </summary>
    /// <remarks>
    /// 补挂工作单元不会让引擎写入进入同一事务（存储自建作用域解析仓储），
    /// 只会凭空多包一层请求级事务，把引擎已提交的状态与请求事务的回滚混在一起。
    /// </remarks>
    [Fact]
    public void WorkflowEndpoints_ShouldNotDeclareUnitOfWork()
    {
        var annotated = DiscoverApplicationServices()
            .SelectMany(type => DiscoverEndpoints(type).Select(method => (Type: type, Method: method)))
            .Where(pair => pair.Method.GetCustomAttribute<UnitOfWorkAttribute>(inherit: false) is not null
                || pair.Type.GetCustomAttribute<UnitOfWorkAttribute>(inherit: false) is not null)
            .Select(pair => $"{pair.Type.Name}.{pair.Method.Name}")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            annotated.Count == 0,
            $"下列工作流接口被挂上了 [UnitOfWork]：{string.Join("、", annotated)}。" +
            $"引擎存储按操作自建作用域持久化，不参与请求事务，挂工作单元只会制造事务边界错觉。");
    }

    /// <summary>
    /// 带复杂查询 DTO 的分页接口必须显式声明 <c>[HttpPost]</c>，否则动态 API 会映射成 GET 并丢掉请求体。
    /// </summary>
    /// <param name="serviceTypeName">查询服务类型名。</param>
    [Theory]
    [InlineData(nameof(WorkflowDefinitionQueryService))]
    [InlineData(nameof(WorkflowInstanceQueryService))]
    [InlineData(nameof(WorkflowTodoQueryService))]
    public void PagedQueryEndpoints_ShouldDeclareHttpPost(string serviceTypeName)
    {
        var serviceType = DiscoverApplicationServices()
            .Single(type => string.Equals(type.Name, serviceTypeName, StringComparison.Ordinal));
        var method = serviceType.GetMethod("GetPageAsync")!;

        Assert.True(
            method.GetCustomAttribute<HttpPostAttribute>(inherit: false) is not null,
            $"{serviceTypeName}.GetPageAsync 缺少 [HttpPost]，分页查询 DTO 会被当成 GET 查询串丢失条件。");
    }

    /// <summary>
    /// 每个接口方法的最后一个参数必须是带默认值的取消令牌，取消才能一路透传到引擎与仓储。
    /// </summary>
    [Fact]
    public void Endpoints_ShouldAcceptOptionalCancellationTokenAsLastParameter()
    {
        var violations = DiscoverApplicationServices()
            .SelectMany(type => DiscoverEndpoints(type).Select(method => (Type: type, Method: method)))
            .Where(pair =>
            {
                var parameters = pair.Method.GetParameters();
                return parameters.Length == 0
                    || parameters[^1].ParameterType != typeof(CancellationToken)
                    || !parameters[^1].HasDefaultValue;
            })
            .Select(pair => $"{pair.Type.Name}.{pair.Method.Name}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列接口的最后一个参数不是带默认值的 CancellationToken：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 六个契约接口的每个方法都必须在实现类上有签名完全一致的公开方法，避免契约与实现悄悄分叉。
    /// </summary>
    [Fact]
    public void Contracts_ShouldMatchImplementationSignatures()
    {
        (Type Contract, Type Implementation)[] pairs =
        [
            (typeof(IWorkflowDefinitionAppService), typeof(WorkflowDefinitionAppService)),
            (typeof(IWorkflowDefinitionQueryService), typeof(WorkflowDefinitionQueryService)),
            (typeof(IWorkflowInstanceAppService), typeof(WorkflowInstanceAppService)),
            (typeof(IWorkflowInstanceQueryService), typeof(WorkflowInstanceQueryService)),
            (typeof(IWorkflowTodoAppService), typeof(WorkflowTodoAppService)),
            (typeof(IWorkflowTodoQueryService), typeof(WorkflowTodoQueryService))
        ];

        var violations = new List<string>();
        foreach (var (contract, implementation) in pairs)
        {
            Assert.True(
                implementation.IsAssignableTo(contract),
                $"{implementation.Name} 未实现契约 {contract.Name}。");

            foreach (var contractMethod in contract.GetMethods())
            {
                var implementationMethod = implementation.GetMethod(
                    contractMethod.Name,
                    [.. contractMethod.GetParameters().Select(parameter => parameter.ParameterType)]);
                if (implementationMethod is null)
                {
                    violations.Add($"{implementation.Name} 缺少 {contractMethod.Name} 的公开实现");
                    continue;
                }

                if (implementationMethod.ReturnType != contractMethod.ReturnType)
                {
                    violations.Add(
                        $"{implementation.Name}.{contractMethod.Name} 返回类型为 {implementationMethod.ReturnType.Name}，" +
                        $"契约为 {contractMethod.ReturnType.Name}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"契约与实现不一致：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 六个契约接口都必须继承 <see cref="IApplicationService"/>，框架据此约定扫描注册。
    /// </summary>
    [Fact]
    public void Contracts_ShouldExtendApplicationServiceMarker()
    {
        Type[] contracts =
        [
            typeof(IWorkflowDefinitionAppService),
            typeof(IWorkflowDefinitionQueryService),
            typeof(IWorkflowInstanceAppService),
            typeof(IWorkflowInstanceQueryService),
            typeof(IWorkflowTodoAppService),
            typeof(IWorkflowTodoQueryService)
        ];

        var violations = contracts
            .Where(contract => !contract.IsAssignableTo(typeof(IApplicationService)))
            .Select(contract => contract.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列契约未继承 IApplicationService，约定扫描不会注册其实现：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 五个应用服务实现都必须是 sealed：它们经动态代理暴露，开放继承会让权限特性被派生类改写。
    /// </summary>
    [Fact]
    public void ApplicationServices_ShouldBeSealed()
    {
        var violations = DiscoverApplicationServices()
            .Where(type => !type.IsSealed)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"下列应用服务不是 sealed：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 发现模块内全部具体应用服务类型。
    /// </summary>
    /// <returns>应用服务类型集合（按名称排序）。</returns>
    private static List<Type> DiscoverApplicationServices()
    {
        return [.. typeof(WorkflowApplicationService).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsAssignableTo(typeof(IApplicationService)))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 发现一个应用服务上被暴露为 HTTP 接口的公开实例方法。
    /// </summary>
    /// <param name="serviceType">应用服务类型。</param>
    /// <returns>接口方法集合。</returns>
    private static List<MethodInfo> DiscoverEndpoints(Type serviceType)
    {
        return [.. serviceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.Name, StringComparer.Ordinal)];
    }
}
