// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限条件参与鉴权后，条件的增删改、启停都让授权快照失效，并记一条授权变更
/// </summary>
public sealed class PermissionConditionAppServiceTests
{
    private readonly Mock<IPermissionConditionDomainService> _domainService = new();
    private readonly Mock<IPermissionConditionQueryService> _queryService = new();
    private readonly Mock<ISaasCacheInvalidator> _cacheInvalidator = new();
    private readonly Mock<IAuthorizationChangeNotifier> _notifier = new();

    /// <summary>
    /// 构造测试上下文：条件 9 绑在角色 3 的权限 5 上
    /// </summary>
    public PermissionConditionAppServiceTests()
    {
        var result = new PermissionConditionCommandResult(9, TargetRoleId: 3, PermissionId: 5);
        _domainService.Setup(service => service.CreatePermissionConditionAsync(It.IsAny<PermissionConditionCreateCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _domainService.Setup(service => service.UpdatePermissionConditionAsync(It.IsAny<PermissionConditionUpdateCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _domainService.Setup(service => service.UpdatePermissionConditionStatusAsync(It.IsAny<PermissionConditionStatusCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _domainService.Setup(service => service.DeletePermissionConditionAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _queryService.Setup(service => service.GetPermissionConditionDetailAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(new PermissionConditionDetailDto());
    }

    /// <summary>
    /// 四种写入都失效授权并记「授权条件变更」，带上绑定的角色与权限
    /// </summary>
    [Fact]
    public async Task EveryWrite_InvalidatesAuthorizationAndAudits()
    {
        var service = new PermissionConditionAppService(_domainService.Object, _queryService.Object, _cacheInvalidator.Object, _notifier.Object);

        await service.CreatePermissionConditionAsync(new PermissionConditionCreateDto { RolePermissionId = 1, AttributeName = "environment.ip", ConditionValue = "10.0.0.0/8" });
        await service.UpdatePermissionConditionAsync(new PermissionConditionUpdateDto { BasicId = 9, RolePermissionId = 1, AttributeName = "environment.ip", ConditionValue = "10.0.0.0/8" });
        await service.UpdatePermissionConditionStatusAsync(new PermissionConditionStatusUpdateDto { BasicId = 9, Status = ValidityStatus.Invalid });
        await service.DeletePermissionConditionAsync(9);

        _cacheInvalidator.Verify(invalidator => invalidator.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Exactly(4));
        _notifier.Verify(
            notifier => notifier.NotifyAsync(PermissionChangeType.ConditionChange, null, 3, 5, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }
}
