// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using XiHan.BasicApp.Printing.Application;
using XiHan.BasicApp.Printing.Application.AppServices;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.QueryServices;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模块动态 API 暴露面的反射型结构约束测试。
/// </summary>
/// <remarks>
/// 这里锁的是「看不见就会漏」的三条仓库传统：写命令必须落在事务工作单元里、
/// 每个暴露方法必须带权限特性、以及 <c>PrintDataSourceQueryService.GetListAsync</c>
/// 这个唯一例外必须保持无特性状态——它在方法体内用 <c>IPermissionChecker</c> 做
/// 「Read 或 Use 二者取一」的命令式判定，并已在 Api.Tests 的自助端点白名单登记。
/// 一旦有人给它补上 <c>PermissionAuthorize</c>，白名单条目就变成死条目、注释也开始说谎，
/// 因此这里反过来断言它没有特性。
/// </remarks>
public sealed class PrintingExtraDynamicApiSurfaceTests
{
    /// <summary>
    /// 命令服务的方法名与其必须携带的权限码。
    /// </summary>
    public static TheoryData<string, string> CommandMethodPermissions =>
        new()
        {
            { nameof(PrintTemplateAppService.CreatePrintTemplateAsync), PrintingPermissionCodes.Create },
            { nameof(PrintTemplateAppService.UpdatePrintTemplateAsync), PrintingPermissionCodes.Update },
            { nameof(PrintTemplateAppService.UpdatePrintTemplateStatusAsync), PrintingPermissionCodes.Status },
            { nameof(PrintTemplateAppService.DeletePrintTemplateAsync), PrintingPermissionCodes.Delete }
        };

    /// <summary>
    /// 查询服务的方法名与其必须携带的权限码。
    /// </summary>
    public static TheoryData<string, string> QueryMethodPermissions =>
        new()
        {
            { nameof(PrintTemplateQueryService.GetPrintTemplatePageAsync), PrintingPermissionCodes.Read },
            { nameof(PrintTemplateQueryService.GetAvailableGlobalPrintTemplatePageAsync), PrintingPermissionCodes.Read },
            { nameof(PrintTemplateQueryService.GetPrintTemplateDetailAsync), PrintingPermissionCodes.Read },
            { nameof(PrintTemplateQueryService.GetResolvedPrintTemplateByCodeAsync), PrintingPermissionCodes.Use }
        };

    /// <summary>
    /// 四个写命令必须逐个带上事务性 <see cref="UnitOfWorkAttribute"/>，否则领域写与缓存失效不在同一个提交边界内。
    /// </summary>
    /// <param name="methodName">被检查的命令方法名。</param>
    /// <param name="permissionCode">该方法应当声明的权限码（此用例不校验，仅用于共享数据集）。</param>
    [Theory]
    [MemberData(nameof(CommandMethodPermissions))]
    public void CommandMethods_ShouldDeclareTransactionalUnitOfWork(string methodName, string permissionCode)
    {
        Assert.False(string.IsNullOrEmpty(permissionCode));
        var method = RequireMethod(typeof(PrintTemplateAppService), methodName);
        var unitOfWork = method.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true);

        Assert.True(unitOfWork is not null, $"{methodName} 缺少 [UnitOfWork]，写路径将脱离事务提交边界。");
        Assert.True(
            unitOfWork!.IsTransactional == true,
            $"{methodName} 的 [UnitOfWork] 不是事务性的，领域写与缓存失效会分裂在两个提交边界。");
    }

    /// <summary>
    /// 四个写命令必须逐个声明对应的权限码，命令与权限的绑定不允许错位。
    /// </summary>
    /// <param name="methodName">被检查的命令方法名。</param>
    /// <param name="permissionCode">该方法必须声明的权限码。</param>
    [Theory]
    [MemberData(nameof(CommandMethodPermissions))]
    public void CommandMethods_ShouldDeclareExpectedPermissionCode(string methodName, string permissionCode)
    {
        AssertSinglePermission(typeof(PrintTemplateAppService), methodName, permissionCode);
    }

    /// <summary>
    /// 查询服务的四个方法必须逐个声明权限码；按编码解析走 use 而不是 read。
    /// </summary>
    /// <param name="methodName">被检查的查询方法名。</param>
    /// <param name="permissionCode">该方法必须声明的权限码。</param>
    [Theory]
    [MemberData(nameof(QueryMethodPermissions))]
    public void QueryMethods_ShouldDeclareExpectedPermissionCode(string methodName, string permissionCode)
    {
        AssertSinglePermission(typeof(PrintTemplateQueryService), methodName, permissionCode);
    }

    /// <summary>
    /// 查询服务不得携带 <see cref="UnitOfWorkAttribute"/>，读路径开事务只会白白占用连接。
    /// </summary>
    [Fact]
    public void QueryService_ShouldNotDeclareUnitOfWork()
    {
        var offenders = PublicApiMethods(typeof(PrintTemplateQueryService))
            .Where(method => method.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true) is not null)
            .Select(method => method.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"查询服务不应带 [UnitOfWork]，违规方法：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 数据源目录端点必须保持「无权限特性」，它的门控在方法体内做 Read 或 Use 的二者取一判定，
    /// 并以此身份登记在 Api.Tests 的自助端点白名单里。
    /// </summary>
    [Fact]
    public void DataSourceCatalog_ShouldStayAttributeFreeToMatchApiWhitelist()
    {
        var method = RequireMethod(typeof(PrintDataSourceQueryService), nameof(PrintDataSourceQueryService.GetListAsync));

        Assert.True(
            method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true).Any() is false,
            "GetListAsync 一旦带上 [PermissionAuthorize]，方法体内的「Read 或 Use」判定与 Api.Tests 白名单条目就同时失真。");
        Assert.True(
            !method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any(),
            "数据源目录含字段契约与样例数据，不得对匿名调用开放。");
    }

    /// <summary>
    /// 三个动态 API 服务都必须继承打印基类，并保留类级 <see cref="AuthorizeAttribute"/> 与 <see cref="DynamicApiAttribute"/>。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [InlineData(typeof(PrintTemplateAppService))]
    [InlineData(typeof(PrintTemplateQueryService))]
    [InlineData(typeof(PrintDataSourceQueryService))]
    public void ApplicationServices_ShouldKeepClassLevelAuthorizeAndDynamicApi(Type serviceType)
    {
        Assert.True(
            serviceType.IsAssignableTo(typeof(PrintingApplicationService)),
            $"{serviceType.Name} 未继承 PrintingApplicationService，会丢掉基类的分组与登录态门控。");
        Assert.True(
            serviceType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(),
            $"{serviceType.Name} 缺少类级 [Authorize]，未登录请求会直接打到方法体。");
        Assert.True(
            serviceType.GetCustomAttributes<DynamicApiAttribute>(inherit: true).Any(),
            $"{serviceType.Name} 缺少 [DynamicApi]，端点不会被暴露。");
        Assert.True(serviceType.IsSealed, $"{serviceType.Name} 应为 sealed，避免派生类绕开类级特性。");
    }

    /// <summary>
    /// 契约接口与实现的方法签名必须逐一对齐，接口先改而实现没跟上时动态 API 会静默少一个端点。
    /// </summary>
    /// <param name="contractType">契约接口类型。</param>
    /// <param name="implementationType">实现类型。</param>
    [Theory]
    [InlineData(typeof(IPrintTemplateAppService), typeof(PrintTemplateAppService))]
    [InlineData(typeof(IPrintTemplateQueryService), typeof(PrintTemplateQueryService))]
    [InlineData(typeof(IPrintDataSourceQueryService), typeof(PrintDataSourceQueryService))]
    public void Contracts_ShouldMatchImplementationSignatures(Type contractType, Type implementationType)
    {
        Assert.True(
            contractType.IsAssignableTo(typeof(IApplicationService)),
            $"{contractType.Name} 未继承 IApplicationService，动态 API 不会扫描到它。");
        Assert.True(
            implementationType.IsAssignableTo(contractType),
            $"{implementationType.Name} 未实现 {contractType.Name}。");

        var missing = new List<string>();
        foreach (var contractMethod in contractType.GetMethods())
        {
            var parameterTypes = contractMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var implementation = implementationType.GetMethod(
                contractMethod.Name,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                parameterTypes,
                modifiers: null);
            if (implementation is null || implementation.ReturnType != contractMethod.ReturnType)
            {
                missing.Add(contractMethod.Name);
            }
        }

        Assert.True(missing.Count == 0, $"{implementationType.Name} 与契约签名不一致的方法：{string.Join("、", missing)}");
    }

    /// <summary>
    /// 契约接口暴露的方法数必须与实现的公开 API 方法数一致，避免实现悄悄多暴露一个未走契约评审的端点。
    /// </summary>
    /// <param name="contractType">契约接口类型。</param>
    /// <param name="implementationType">实现类型。</param>
    [Theory]
    [InlineData(typeof(IPrintTemplateAppService), typeof(PrintTemplateAppService))]
    [InlineData(typeof(IPrintTemplateQueryService), typeof(PrintTemplateQueryService))]
    [InlineData(typeof(IPrintDataSourceQueryService), typeof(PrintDataSourceQueryService))]
    public void Implementations_ShouldNotExposeMethodsOutsideContract(Type contractType, Type implementationType)
    {
        var contractNames = contractType.GetMethods().Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        var extras = PublicApiMethods(implementationType)
            .Select(method => method.Name)
            .Where(name => !contractNames.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(extras.Count == 0, $"{implementationType.Name} 暴露了契约之外的公开方法：{string.Join("、", extras)}");
    }

    /// <summary>
    /// 每个暴露方法的最后一个参数都必须是带默认值的取消令牌，取消信号才能一路透传到仓储。
    /// </summary>
    /// <param name="serviceType">被检查的服务类型。</param>
    [Theory]
    [InlineData(typeof(PrintTemplateAppService))]
    [InlineData(typeof(PrintTemplateQueryService))]
    [InlineData(typeof(PrintDataSourceQueryService))]
    public void ApiMethods_ShouldEndWithOptionalCancellationToken(Type serviceType)
    {
        var offenders = new List<string>();
        foreach (var method in PublicApiMethods(serviceType))
        {
            var parameters = method.GetParameters();
            var last = parameters.Length > 0 ? parameters[^1] : null;
            if (last is null || last.ParameterType != typeof(CancellationToken) || !last.HasDefaultValue)
            {
                offenders.Add(method.Name);
            }
        }

        Assert.True(offenders.Count == 0, $"{serviceType.Name} 缺少可选取消令牌尾参的方法：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 断言指定方法有且只有一个权限特性，且权限码与预期一致、不叠加 ABAC 策略。
    /// </summary>
    private static void AssertSinglePermission(Type serviceType, string methodName, string permissionCode)
    {
        var method = RequireMethod(serviceType, methodName);
        var permissions = method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true).ToList();

        Assert.True(permissions.Count == 1, $"{serviceType.Name}.{methodName} 的 [PermissionAuthorize] 数量为 {permissions.Count}，应恰好为 1。");
        Assert.Equal(permissionCode, permissions[0].PermissionCode);
        Assert.True(
            string.IsNullOrWhiteSpace(permissions[0].AbacPolicyCode),
            $"{serviceType.Name}.{methodName} 叠加了 ABAC 策略 {permissions[0].AbacPolicyCode}，打印模板不使用属性级策略。");
    }

    /// <summary>
    /// 取得服务上会被动态 API 暴露的公开实例方法（排除对象基类成员与属性访问器）。
    /// </summary>
    private static IEnumerable<MethodInfo> PublicApiMethods(Type serviceType)
    {
        return serviceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);
    }

    /// <summary>
    /// 取得必须存在的方法，缺失时给出可定位的失败消息。
    /// </summary>
    private static MethodInfo RequireMethod(Type serviceType, string methodName)
    {
        return serviceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{serviceType.Name} 上未找到方法 {methodName}。");
    }
}
