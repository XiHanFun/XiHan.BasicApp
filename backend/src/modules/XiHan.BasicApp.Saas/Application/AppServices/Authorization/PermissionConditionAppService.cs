// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限条件命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限条件")]
public sealed class PermissionConditionAppService
    : SaasApplicationService, IPermissionConditionAppService
{
    /// <summary>
    /// 权限 ABAC 条件领域服务
    /// </summary>
    private readonly IPermissionConditionDomainService _permissionConditionDomainService;

    /// <summary>
    /// 权限 ABAC 条件查询服务
    /// </summary>
    private readonly IPermissionConditionQueryService _permissionConditionQueryService;

    /// <summary>
    /// 缓存失效器
    /// </summary>
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 授权变更通知
    /// </summary>
    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionAppService(
        IPermissionConditionDomainService permissionConditionDomainService,
        IPermissionConditionQueryService permissionConditionQueryService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier)
    {
        _permissionConditionDomainService = permissionConditionDomainService;
        _permissionConditionQueryService = permissionConditionQueryService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
    }

    #region PermissionCondition

    /// <summary>
    /// 创建权限 ABAC 条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Create)]
    public async Task<PermissionConditionDetailDto> CreatePermissionConditionAsync(PermissionConditionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionConditionDomainService.CreatePermissionConditionAsync(PermissionConditionApplicationMapper.ToCreateCommand(input), cancellationToken);
        await OnConditionChangedAsync(result, $"新增条件 {input.AttributeName.Trim()}", cancellationToken);

        return await _permissionConditionQueryService.GetPermissionConditionDetailAsync(result.ConditionId, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }

    /// <summary>
    /// 删除权限 ABAC 条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Delete)]
    public async Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionConditionDomainService.DeletePermissionConditionAsync(id, cancellationToken);
        await OnConditionChangedAsync(result, "删除条件", cancellationToken);
    }

    /// <summary>
    /// 更新权限 ABAC 条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Update)]
    public async Task<PermissionConditionDetailDto> UpdatePermissionConditionAsync(PermissionConditionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionConditionDomainService.UpdatePermissionConditionAsync(PermissionConditionApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await OnConditionChangedAsync(result, $"修改条件 {input.AttributeName.Trim()}", cancellationToken);

        return await _permissionConditionQueryService.GetPermissionConditionDetailAsync(result.ConditionId, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }

    /// <summary>
    /// 更新权限 ABAC 条件状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Status)]
    public async Task<PermissionConditionDetailDto> UpdatePermissionConditionStatusAsync(PermissionConditionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionConditionDomainService.UpdatePermissionConditionStatusAsync(PermissionConditionApplicationMapper.ToStatusCommand(input), cancellationToken);
        await OnConditionChangedAsync(result, $"条件改为{(input.Status == ValidityStatus.Valid ? "启用" : "停用")}", cancellationToken);

        return await _permissionConditionQueryService.GetPermissionConditionDetailAsync(result.ConditionId, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }

    #endregion PermissionCondition

    /// <summary>
    /// 条件参与鉴权：任何写入都让授权快照失效（事务提交后生效），并记一条授权变更
    /// </summary>
    private async Task OnConditionChangedAsync(PermissionConditionCommandResult result, string reason, CancellationToken cancellationToken)
    {
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _authorizationChangeNotifier.NotifyAsync(
            PermissionChangeType.ConditionChange,
            targetUserId: result.TargetUserId,
            targetRoleId: result.TargetRoleId,
            permissionId: result.PermissionId,
            reason: reason,
            cancellationToken: cancellationToken);
    }
}
