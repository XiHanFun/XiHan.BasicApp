// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using XiHan.BasicApp.Saas.Infrastructure.Logging;
using XiHan.Framework.Auditing;
using XiHan.Framework.Web.Api.DynamicApi.Conventions;
using XiHan.Framework.Web.Api.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 动态 API 端点的操作日志归类测试。
/// </summary>
/// <remarks>
/// 操作日志写入器只拿得到路由层的控制器名、动作名与 HTTP 方法，它按控制器名后缀 <c>Query</c>
/// 认出查询服务（<c>XxxQueryService</c>）并整组不记。本类用运行期生成控制器的同一份约定逐端点推出路由名，
/// 守住这条约定的两头：查询服务的端点一个不漏地排除在外；被整组排除的只能是只读服务，
/// 命令服务不能因为命名撞上后缀而静默丢失审计。
/// </remarks>
public sealed class OperationLogClassificationTests
{
    /// <summary>
    /// 不按 <c>XxxQueryService</c> 命名、控制器名却以 <c>Query</c> 结尾的只读服务，元素为类名，须注明为何只读。
    /// </summary>
    private static readonly IReadOnlySet<string> ReadOnlyServicesOutsideQueryServices =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // RAG 检索问答：检索片段后交会话生成答案，不写业务数据
            "KnowledgeQueryAppService"
        };

    /// <summary>
    /// 与运行期生成控制器同一份动态 API 约定（取自框架的 MVC 装配）。
    /// </summary>
    private static readonly IDynamicApiConvention RuntimeConvention = ResolveRuntimeConvention();

    /// <summary>
    /// 查询服务的每个端点都不进操作日志：不论走 GET 还是带复杂条件走 POST，也不论动作名带不带查询语义。
    /// </summary>
    [Fact]
    public void QueryServiceEndpoints_ShouldStayOutOfOperationLog()
    {
        var endpoints = EndpointAuthorizationCoverageTests.EnumerateExposedEndpoints()
            .Where(endpoint => IsQueryService(endpoint.Service))
            .ToList();
        Assert.True(endpoints.Count > 100, $"只发现了 {endpoints.Count} 个查询服务端点，扫描条件可能失效了。");

        var recorded = endpoints
            .Select(endpoint => (endpoint.Service, endpoint.Method, Record: ToRecord(endpoint.Service, endpoint.Method)))
            .Where(item => SaasOperationLogWriter.ResolveRecordedOperationType(item.Record) is not null)
            .Select(item => $"{item.Service.Name}.{item.Method.Name} → {item.Record.Method} {item.Record.Path}")
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(recorded.Count == 0,
            $"下列 {recorded.Count} 个查询服务端点会被记进操作日志：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, recorded)}");
    }

    /// <summary>
    /// 被整组按查询排除的控制器只能属于只读服务：用控制器名配一个写动作探测，命令服务必须照常落库。
    /// </summary>
    [Fact]
    public void ControllersExcludedAsQuery_ShouldBelongToReadOnlyServices()
    {
        var excluded = EndpointAuthorizationCoverageTests.EnumerateExposedEndpoints()
            .Select(endpoint => endpoint.Service)
            .Distinct()
            .Where(service => !IsQueryService(service))
            .Where(IsExcludedAsQueryController)
            .Select(service => service.Name)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = excluded
            .Where(name => !ReadOnlyServicesOutsideQueryServices.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(offenders.Count == 0,
            $"下列服务的控制器名以 Query 结尾，其写操作会被当成查询整组漏记操作日志，请改名，或确认只读后登记：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");

        var stale = ReadOnlyServicesOutsideQueryServices
            .Where(name => !excluded.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(stale.Count == 0,
            $"下列名单条目已失效（服务不存在，或控制器名已不按查询排除），请移除：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    /// <summary>
    /// 是否查询服务（读写分离约定下只承载读的 <c>XxxQueryService</c>）。
    /// </summary>
    private static bool IsQueryService(Type service)
    {
        return service.Name.EndsWith("QueryService", StringComparison.Ordinal);
    }

    /// <summary>
    /// 该服务生成的控制器是否被操作日志整组按查询排除：配上写动作 <c>Create</c> 仍不落库即是。
    /// </summary>
    private static bool IsExcludedAsQueryController(Type service)
    {
        var context = new DynamicApiConventionContext { ServiceType = service };
        RuntimeConvention.Apply(context);
        var controllerName = context.ControllerName ?? service.Name;

        var probe = new OperationLogRecord
        {
            ControllerName = controllerName,
            ActionName = "Create",
            Method = "POST",
            Path = $"/api/{controllerName}/Create"
        };
        return SaasOperationLogWriter.ResolveRecordedOperationType(probe) is null;
    }

    /// <summary>
    /// 按运行期约定推出端点的路由名，组装成过滤器会交给写入器的记录。
    /// </summary>
    /// <remarks>控制器名与 HTTP 方法的缺省值同 <c>DynamicApiControllerFactory</c>。</remarks>
    private static OperationLogRecord ToRecord(Type service, MethodInfo method)
    {
        var context = new DynamicApiConventionContext { ServiceType = service, MethodInfo = method };
        RuntimeConvention.Apply(context);
        var controllerName = context.ControllerName ?? service.Name;
        var actionName = context.ActionName ?? method.Name;

        return new OperationLogRecord
        {
            ControllerName = controllerName,
            ActionName = actionName,
            Method = context.HttpMethod ?? "POST",
            Path = $"/api/{controllerName}/{actionName}"
        };
    }

    /// <summary>
    /// 取框架 MVC 装配注册的动态 API 约定，命名选项与运行期一致。
    /// </summary>
    private static IDynamicApiConvention ResolveRuntimeConvention()
    {
        var services = new ServiceCollection();
        services.AddXiHanWebApiMvc();

        return services
            .Where(descriptor => descriptor.ServiceType == typeof(IDynamicApiConvention))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<IDynamicApiConvention>()
            .Single();
    }
}
