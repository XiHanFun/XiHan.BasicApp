// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using XiHan.BasicApp.AI.Application;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 模块动态 API 暴露面的结构约束测试（反射型，不起宿主）。
/// </summary>
/// <remarks>
/// 应用服务是靠 <c>[DynamicApi]</c> 自动变成 HTTP 端点的：类上少一个特性接口就消失，
/// 方法上少一个 <c>[PermissionAuthorize]</c> 端点就变成"登录即可调"——两种失误都不会有任何编译或启动报错，
/// 只能靠本文件把暴露面钉死。放行名单在下方显式登记，新增免鉴权端点必须先改这里。
/// </remarks>
public sealed class AiApplicationSurfaceStructureTests
{
    /// <summary>
    /// 显式放行的免权限端点（仅凭登录态即可调用），键为「类型名.方法名」。
    /// </summary>
    /// <remarks>
    /// - 聊天助手两个端点面向普通登录用户，用助手不看管理侧权限，只看登录态与会话归属；
    /// - 可用助手列表是聊天页的下拉数据源，同理不挂管理权限。
    /// 新增任何一条都意味着放开一个免鉴权端点，必须在评审里单独说明。
    /// </remarks>
    private static readonly string[] PermissionExemptEndpoints =
    [
        "ChatAssistantAppService.OpenConversationAsync",
        "ChatAssistantAppService.ReplyAsync",
        "AiAssistantQueryService.GetAvailableAsync"
    ];

    /// <summary>
    /// 显式放行的免工作单元写端点，键为「类型名.方法名」。
    /// </summary>
    /// <remarks>
    /// 知识文档删除要先清向量库再软删元信息，含外部网络 I/O；套上工作单元会把网络往返关进长事务。
    /// 源码类注释已写明这条取舍，本清单是它的可执行版本。
    /// </remarks>
    private static readonly string[] UnitOfWorkExemptWriteEndpoints =
    [
        "KnowledgeDocumentAppService.DeleteAsync"
    ];

    /// <summary>
    /// 写操作方法名前缀（命中即要求工作单元）。
    /// </summary>
    private static readonly string[] WriteMethodPrefixes = ["Create", "Update", "Delete", "SetDefault"];

    /// <summary>
    /// 模块内全部动态 API 服务类型。
    /// </summary>
    public static TheoryData<Type> AllApplicationServiceTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in ApplicationServiceTypes())
            {
                data.Add(type);
            }

            return data;
        }
    }

    /// <summary>
    /// 模块里必须真的存在一批动态 API 服务，否则本文件的全部 Theory 会因空数据而"假通过"。
    /// </summary>
    [Fact]
    public void ModuleAssembly_ShouldExposeApplicationServices()
    {
        Assert.Equal(10, ApplicationServiceTypes().Count);
    }

    /// <summary>
    /// 应用服务基类必须同时带 <see cref="AuthorizeAttribute"/> 与 <c>[DynamicApi]</c>：
    /// 前者保证默认需要登录，后者保证整组服务被暴露成 HTTP API 并落在独立分组里。
    /// </summary>
    [Fact]
    public void ApplicationServiceBase_ShouldRequireAuthenticationAndDeclareGroup()
    {
        var baseType = typeof(AiApplicationService);
        var dynamicApi = baseType.GetCustomAttribute<DynamicApiAttribute>();

        Assert.NotNull(baseType.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(dynamicApi);
        Assert.Equal("BasicApp.AI", dynamicApi!.Group, StringComparer.Ordinal);
        Assert.Equal("AI 服务", dynamicApi.GroupName, StringComparer.Ordinal);
        Assert.True(baseType.IsAbstract, "应用服务基类必须是抽象类，避免被误当成一个可暴露的端点容器。");
    }

    /// <summary>
    /// 每个应用服务都必须自带 <c>[DynamicApi]</c> 并落在同一分组，同时给出非空 Tag（Scalar 文档按 Tag 分节）。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_ShouldDeclareDynamicApiWithGroupAndTag(Type serviceType)
    {
        var dynamicApi = serviceType.GetCustomAttribute<DynamicApiAttribute>(inherit: false);

        Assert.True(dynamicApi is not null, $"{serviceType.Name} 缺少类级 [DynamicApi]，不会被暴露为 HTTP API。");
        Assert.Equal("BasicApp.AI", dynamicApi!.Group, StringComparer.Ordinal);
        Assert.Equal("AI 服务", dynamicApi.GroupName, StringComparer.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(dynamicApi.Tag), $"{serviceType.Name} 的 [DynamicApi] 未指定 Tag。");
        Assert.True(dynamicApi.IsEnabled, $"{serviceType.Name} 的动态 API 被显式关闭。");
    }

    /// <summary>
    /// 每个应用服务都必须是 sealed 且继承自本模块基类，避免绕开基类的 <c>[Authorize]</c> 默认策略。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_ShouldBeSealedAndDeriveFromModuleBase(Type serviceType)
    {
        Assert.True(serviceType.IsSealed, $"{serviceType.Name} 不是 sealed，可能被派生出绕过鉴权的子类。");
        Assert.True(
            serviceType.IsAssignableTo(typeof(AiApplicationService)),
            $"{serviceType.Name} 未继承 AiApplicationService，会丢掉类级 [Authorize] 与分组。");
    }

    /// <summary>
    /// 每个暴露出去的公共方法都必须挂 <c>[PermissionAuthorize]</c>，除非登记在免权限放行名单里。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_PublicEndpoints_ShouldCarryPermissionAuthorize(Type serviceType)
    {
        var violations = PublicEndpoints(serviceType)
            .Where(method => method.GetCustomAttribute<PermissionAuthorizeAttribute>() is null)
            .Select(method => $"{serviceType.Name}.{method.Name}")
            .Where(key => !PermissionExemptEndpoints.Contains(key, StringComparer.Ordinal))
            .ToList();

        Assert.True(violations.Count == 0, $"下列端点缺少 [PermissionAuthorize]，会变成「登录即可调」：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 端点上写的权限码必须是本模块四类权限常量里真实存在的值，写错字符串只会在运行时静默 403。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_PermissionCodes_ShouldExistInPermissionConstants(Type serviceType)
    {
        var known = KnownPermissionCodes();
        var unknown = PublicEndpoints(serviceType)
            .Select(method => (method.Name, Attribute: method.GetCustomAttribute<PermissionAuthorizeAttribute>()))
            .Where(item => item.Attribute is not null && !known.Contains(item.Attribute!.PermissionCode))
            .Select(item => $"{serviceType.Name}.{item.Name}={item.Attribute!.PermissionCode}")
            .ToList();

        Assert.True(unknown.Count == 0, $"下列端点绑定了不存在的权限码：{string.Join("；", unknown)}。");
    }

    /// <summary>
    /// 写操作必须套 <c>[UnitOfWork(true)]</c>，除非登记在免工作单元名单里；漏了会让多表写入半途失败留下脏数据。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_WriteEndpoints_ShouldBeWrappedInUnitOfWork(Type serviceType)
    {
        var violations = PublicEndpoints(serviceType)
            .Where(method => WriteMethodPrefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal)))
            .Where(method => method.GetCustomAttribute<UnitOfWorkAttribute>() is null)
            .Select(method => $"{serviceType.Name}.{method.Name}")
            .Where(key => !UnitOfWorkExemptWriteEndpoints.Contains(key, StringComparer.Ordinal))
            .ToList();

        Assert.True(violations.Count == 0, $"下列写端点缺少 [UnitOfWork]：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 免工作单元与免权限的放行名单必须始终指向真实存在的端点，端点改名后名单会静默失效、约束随之消失。
    /// </summary>
    [Fact]
    public void ExemptionLists_ShouldOnlyReferenceExistingEndpoints()
    {
        var existing = ApplicationServiceTypes()
            .SelectMany(type => PublicEndpoints(type).Select(method => $"{type.Name}.{method.Name}"))
            .ToHashSet(StringComparer.Ordinal);
        var stale = PermissionExemptEndpoints
            .Concat(UnitOfWorkExemptWriteEndpoints)
            .Where(key => !existing.Contains(key))
            .ToList();

        Assert.True(stale.Count == 0, $"放行名单里的下列端点已不存在，请同步清理：{string.Join("、", stale)}。");
    }

    /// <summary>
    /// 每个公共端点的最后一个参数都必须是带默认值的 <see cref="CancellationToken"/>，否则调用方无法取消长耗时的模型请求。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_PublicEndpoints_ShouldAcceptOptionalCancellationToken(Type serviceType)
    {
        var violations = PublicEndpoints(serviceType)
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 0
                    || parameters[^1].ParameterType != typeof(CancellationToken)
                    || !parameters[^1].HasDefaultValue;
            })
            .Select(method => $"{serviceType.Name}.{method.Name}")
            .ToList();

        Assert.True(violations.Count == 0, $"下列端点未以可选 CancellationToken 收尾：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 每个应用服务必须恰好实现一个本模块契约接口，且公共端点集合与契约完全一致——
    /// 多出的公共方法会被动态 API 一并暴露成没写进契约的野端点。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [MemberData(nameof(AllApplicationServiceTypes))]
    public void ApplicationService_PublicSurface_ShouldMatchItsContract(Type serviceType)
    {
        var contracts = serviceType
            .GetInterfaces()
            .Where(item => string.Equals(item.Namespace, "XiHan.BasicApp.AI.Application.Contracts", StringComparison.Ordinal))
            .ToList();

        Assert.True(contracts.Count == 1, $"{serviceType.Name} 应恰好实现一个契约接口，实际 {contracts.Count} 个。");

        var contractMethods = contracts[0]
            .GetMethods()
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var exposedMethods = PublicEndpoints(serviceType)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(contractMethods, exposedMethods, StringComparer.Ordinal);
    }

    /// <summary>
    /// 契约接口必须只声明异步方法（返回 Task），同步阻塞方法会在动态 API 管道里占满线程池。
    /// </summary>
    [Fact]
    public void Contracts_ShouldOnlyDeclareAsyncMethods()
    {
        var methods = typeof(AiApplicationService).Assembly
            .GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => string.Equals(type.Namespace, "XiHan.BasicApp.AI.Application.Contracts", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods().Select(method => (Owner: type.Name, method.Name, method.ReturnType)))
            .ToList();
        var violations = methods
            .Where(item => !item.ReturnType.IsAssignableTo(typeof(Task)))
            .Select(item => $"{item.Owner}.{item.Name}")
            .ToList();

        Assert.NotEmpty(methods);
        Assert.True(violations.Count == 0, $"下列契约方法不是异步方法：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 契约方法名必须以 <c>Async</c> 收尾，前端 API 模块与动态路由都按这个约定生成。
    /// </summary>
    [Fact]
    public void Contracts_MethodNames_ShouldEndWithAsync()
    {
        var methods = typeof(AiApplicationService).Assembly
            .GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => string.Equals(type.Namespace, "XiHan.BasicApp.AI.Application.Contracts", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods().Select(method => (Owner: type.Name, method.Name)))
            .ToList();
        var violations = methods
            .Where(item => !item.Name.EndsWith("Async", StringComparison.Ordinal))
            .Select(item => $"{item.Owner}.{item.Name}")
            .ToList();

        Assert.NotEmpty(methods);
        Assert.True(violations.Count == 0, $"下列契约方法名未以 Async 结尾：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// 取模块内全部动态 API 服务类型。
    /// </summary>
    /// <returns>服务类型清单。</returns>
    private static IReadOnlyList<Type> ApplicationServiceTypes()
    {
        return [.. typeof(AiApplicationService).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsAssignableTo(typeof(AiApplicationService)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 取某个服务上会被暴露成端点的公共方法（本类声明、非特殊名、排除构造与属性访问器）。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    /// <returns>端点方法清单。</returns>
    private static IReadOnlyList<MethodInfo> PublicEndpoints(Type serviceType)
    {
        return [.. serviceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 取本模块四类权限常量里的全部权限码。
    /// </summary>
    /// <returns>权限码集合。</returns>
    private static HashSet<string> KnownPermissionCodes()
    {
        Type[] codeTypes =
        [
            typeof(AiPermissionCodes),
            typeof(AiAssistantPermissionCodes),
            typeof(AiPromptPermissionCodes),
            typeof(KnowledgePermissionCodes)
        ];

        return [.. codeTypes
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)];
    }
}
