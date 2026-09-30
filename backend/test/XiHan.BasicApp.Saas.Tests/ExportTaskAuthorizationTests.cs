// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.BasicApp.Saas.Infrastructure.Exporting;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 导出授权测试：导出按导出权限校验，而不是资源的读权限；提交任务时就拦截。
/// </summary>
/// <remarks>
/// 回归锚点：Provider 曾按 <c>.Read</c> 校验、提交接口对所有登录用户开放，
/// 页面导出按钮却挂 <c>.Export</c>——只有读权限的人直调提交接口就能导出，按钮门控形同虚设。
/// </remarks>
public sealed class ExportTaskAuthorizationTests
{
    private const long CurrentUserId = 42;

    /// <summary>
    /// SaaS 模块登记的全部导出 Provider
    /// </summary>
    public static TheoryData<Type> RegisteredProviders { get; } = [.. new ServiceCollection()
        .AddSaasExportInfrastructure()
        .Where(descriptor => descriptor.ServiceType == typeof(IExportProvider))
        .Select(descriptor => descriptor.ImplementationType!)];

    /// <summary>
    /// 业务类型就是页面码，页面上有 <c>{业务类型}.export</c> 导出按钮，Provider 的导出权限与该按钮绑定的是同一个码
    /// </summary>
    /// <remarks>
    /// 回归锚点：用户导出的业务类型曾是 system.user，与页面码 identity.user 对不上，只能靠人工对照表维护。
    /// </remarks>
    /// <param name="providerType">Provider 类型</param>
    [Theory]
    [MemberData(nameof(RegisteredProviders))]
    public void Provider_ShouldMatchPageExportButton(Type providerType)
    {
        var provider = CreateProvider(providerType);

        Assert.Contains(PageRegistry.All, page => page.Code == provider.BusinessType);
        var button = Assert.Single(PageRegistry.Buttons, item => item.Code == $"{provider.BusinessType}.export");
        Assert.Equal(provider.BusinessType, button.ParentCode);
        Assert.Equal(button.PermissionCode, provider.RequiredPermission);
        Assert.EndsWith(":export", provider.RequiredPermission, StringComparison.Ordinal);
    }

    /// <summary>
    /// 只有读权限、没有导出权限：提交即拒绝，任务不落库、不入队
    /// </summary>
    [Fact]
    public async Task Submit_WithReadButNotExportPermission_ShouldRejectBeforePersisting()
    {
        var fixture = new SubmitFixture(granted: [SaasPermissionCodes.User.Read]);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.SubmitAsync(SubmitInput("identity.user")));

        Assert.Contains("用户导出", exception.Message);
        fixture.Repository.Verify(repo => repo.AddAsync(It.IsAny<SysExportTask>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Queue.Verify(queue => queue.EnqueueAsync(It.IsAny<ExportTaskMessage>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 持有导出权限：按 Provider 的导出权限校验后落库并入队；业务类型与执行器同样不区分大小写
    /// </summary>
    /// <param name="businessType">提交的业务类型</param>
    [Theory]
    [InlineData("identity.user")]
    [InlineData("  Identity.User ")]
    public async Task Submit_WithExportPermission_ShouldPersistAndEnqueue(string businessType)
    {
        var fixture = new SubmitFixture(granted: [SaasPermissionCodes.User.Export]);

        var result = await fixture.Service.SubmitAsync(SubmitInput(businessType));

        Assert.Equal(ExportTaskStatus.Pending, result.Status);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(CurrentUserId.ToString(), SaasPermissionCodes.User.Export, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Repository.Verify(repo => repo.AddAsync(It.IsAny<SysExportTask>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Queue.Verify(queue => queue.EnqueueAsync(It.IsAny<ExportTaskMessage>(), TimeSpan.Zero, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 未接入导出的业务类型：提交即拒绝，不做权限判定也不落库
    /// </summary>
    [Fact]
    public async Task Submit_UnknownBusinessType_ShouldRejectBeforePersisting()
    {
        var fixture = new SubmitFixture(granted: [SaasPermissionCodes.User.Export]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.SubmitAsync(SubmitInput("identity.unknown")));

        Assert.Contains("未接入导出", exception.Message);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Repository.Verify(repo => repo.AddAsync(It.IsAny<SysExportTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IExportProvider CreateProvider(Type providerType)
    {
        var constructor = Assert.Single(providerType.GetConstructors());
        var arguments = constructor.GetParameters()
            .Select(parameter => ((Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(parameter.ParameterType))!).Object)
            .ToArray();
        return Assert.IsAssignableFrom<IExportProvider>(constructor.Invoke(arguments));
    }

    private static ExportTaskSubmitDto SubmitInput(string businessType)
    {
        return new ExportTaskSubmitDto
        {
            BusinessType = businessType,
            TaskName = "用户导出",
            Columns = [new ExportColumnDto { Key = "userName", Title = "用户名" }]
        };
    }

    private sealed class SubmitFixture
    {
        public SubmitFixture(IReadOnlyCollection<string> granted)
        {
            var currentUser = new Mock<ICurrentUser>();
            _ = currentUser.SetupGet(user => user.UserId).Returns(CurrentUserId);

            _ = PermissionChecker
                .Setup(checker => checker.IsGrantedAsync(CurrentUserId.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, string permissionCode, CancellationToken _) => granted.Contains(permissionCode));

            _ = Repository
                .Setup(repo => repo.AddAsync(It.IsAny<SysExportTask>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysExportTask task, CancellationToken _) =>
                {
                    SaasTestHelper.SetBasicId(task, 5);
                    return task;
                });

            Service = new ExportTaskAppService(
                Repository.Object,
                currentUser.Object,
                Queue.Object,
                // 无环境 UoW（Current 为 null）：提交后直接入队
                new Mock<IUnitOfWorkManager>().Object,
                [new UserExportProvider(Mock.Of<IUserQueryService>())],
                new OperationPermissionGuard(currentUser.Object, PermissionChecker.Object));
        }

        public ExportTaskAppService Service { get; }

        public Mock<IExportTaskRepository> Repository { get; } = new();

        public Mock<IRedisDelayQueue<ExportTaskMessage>> Queue { get; } = new();

        public Mock<IPermissionChecker> PermissionChecker { get; } = new();
    }
}
