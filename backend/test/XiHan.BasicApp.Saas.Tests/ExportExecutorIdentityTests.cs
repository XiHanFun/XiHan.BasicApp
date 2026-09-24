// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Security.Claims;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 后台导出按发起时的身份执行，与在线请求同一口径：会话声明（会话失效即拒绝）、角色（与签发令牌同一来源）、
/// 模仿者声明（模仿态禁用的权限照样禁用）；租户不可用时直接失败。
/// </summary>
public sealed class ExportExecutorIdentityTests
{
    private const long TenantId = 7;
    private const long UserId = 42;

    /// <summary>
    /// 权限校验时的主体带着发起会话、角色与模仿者
    /// </summary>
    [Fact]
    public async Task Execute_RebuildsPrincipalFromRequester()
    {
        var fixture = new Fixture();
        var task = fixture.Task(sessionId: "session-1", impersonatorUserId: 9, impersonatorTenantId: 0);

        await fixture.Executor.ExecuteAsync(task);

        var principal = Assert.IsType<ClaimsPrincipal>(fixture.PrincipalAtPermissionCheck);
        Assert.Equal("session-1", principal.FindFirst(XiHanClaimTypes.SessionId)?.Value);
        Assert.Contains(principal.FindAll(XiHanClaimTypes.Role), claim => claim.Value == "tenant_admin");
        Assert.Equal("9", principal.FindFirst(XiHanClaimTypes.ImpersonatorUserId)?.Value);
        Assert.Equal(UserId.ToString(), principal.FindFirst(XiHanClaimTypes.UserId)?.Value);
    }

    /// <summary>
    /// 非会话型发起（没有会话声明）不带会话声明，也不带模仿者
    /// </summary>
    [Fact]
    public async Task Execute_WithoutSession_OmitsSessionAndImpersonator()
    {
        var fixture = new Fixture();

        await fixture.Executor.ExecuteAsync(fixture.Task(sessionId: null));

        var principal = Assert.IsType<ClaimsPrincipal>(fixture.PrincipalAtPermissionCheck);
        Assert.Null(principal.FindFirst(XiHanClaimTypes.SessionId));
        Assert.Null(principal.FindFirst(XiHanClaimTypes.ImpersonatorUserId));
    }

    /// <summary>
    /// 租户停用时不再导出，也不做权限校验
    /// </summary>
    [Fact]
    public async Task Execute_TenantUnavailable_Fails()
    {
        var fixture = new Fixture(tenantStatus: TenantStatus.Disabled);

        await fixture.Executor.ExecuteAsync(fixture.Task(sessionId: "session-1"));

        fixture.Repository.Verify(
            repo => repo.MarkFailedAsync(5, It.Is<string>(message => message.Contains("租户当前不可用")), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class Fixture
    {
        private readonly RecordingPrincipalAccessor _principalAccessor = new();

        public Fixture(TenantStatus tenantStatus = TenantStatus.Normal)
        {
            var provider = new Mock<IExportProvider>();
            _ = provider.SetupGet(item => item.BusinessType).Returns("system.user");
            _ = provider.SetupGet(item => item.RequiredPermission).Returns("identity.user.export");

            var snapshot = new Mock<IAuthorizationSnapshotQueryService>();
            _ = snapshot
                .Setup(service => service.BuildAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationSnapshot(["tenant_admin"], [], [], []));

            var tenant = new SysTenant
            {
                TenantStatus = tenantStatus,
                ConfigStatus = TenantConfigStatus.Configured
            };
            SaasTestHelper.SetBasicId(tenant, TenantId);
            var tenants = new Mock<ITenantRepository>();
            _ = tenants.Setup(repo => repo.GetByIdAsync(TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

            // 权限校验处记下当时的主体后拒绝，后续写出流程不在本测试范围
            _ = PermissionChecker
                .Setup(checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => PrincipalAtPermissionCheck = _principalAccessor.Principal)
                .ReturnsAsync(false);

            Executor = new ExportExecutor(
                [provider.Object],
                [],
                Repository.Object,
                new Mock<IFileTransferService>().Object,
                new TestCurrentTenant(),
                _principalAccessor,
                PermissionChecker.Object,
                new Mock<IUserTaskProgressNotifier>().Object,
                NullLogger<ExportExecutor>.Instance,
                snapshot.Object,
                tenants.Object);
        }

        public ExportExecutor Executor { get; }

        public Mock<IExportTaskRepository> Repository { get; } = new();

        public Mock<IPermissionChecker> PermissionChecker { get; } = new();

        public ClaimsPrincipal? PrincipalAtPermissionCheck { get; private set; }

        public SysExportTask Task(string? sessionId, long? impersonatorUserId = null, long? impersonatorTenantId = null)
        {
            var task = new SysExportTask
            {
                TenantId = TenantId,
                BusinessType = "system.user",
                TaskName = "用户导出",
                FieldsSnapshot = "[]",
                RequesterSessionId = sessionId,
                ImpersonatorUserId = impersonatorUserId,
                ImpersonatorTenantId = impersonatorTenantId
            };
            SaasTestHelper.SetBasicId(task, 5);
            typeof(SysExportTask).GetProperty(nameof(SysExportTask.CreatedId))!.SetValue(task, UserId);
            return task;
        }
    }

    private sealed class RecordingPrincipalAccessor : ICurrentPrincipalAccessor
    {
        private ClaimsPrincipal _principal = new();

        public ClaimsPrincipal Principal => _principal;

        public IDisposable Change(ClaimsPrincipal principal)
        {
            var previous = _principal;
            _principal = principal;
            return new Restore(() => _principal = previous);
        }

        private sealed class Restore(Action action) : IDisposable
        {
            public void Dispose() => action();
        }
    }
}
