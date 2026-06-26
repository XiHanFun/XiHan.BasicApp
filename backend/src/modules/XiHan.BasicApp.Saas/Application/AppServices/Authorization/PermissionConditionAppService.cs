#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionAppService
// Guid:9e5a2c7f-6d8b-4e0a-1b2c-5d3e4f6a7b8c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
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
    /// 构造函数
    /// </summary>
    public PermissionConditionAppService(
        IPermissionConditionDomainService permissionConditionDomainService,
        IPermissionConditionQueryService permissionConditionQueryService)
    {
        _permissionConditionDomainService = permissionConditionDomainService;
        _permissionConditionQueryService = permissionConditionQueryService;
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

        await _permissionConditionDomainService.DeletePermissionConditionAsync(id, cancellationToken);
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

        return await _permissionConditionQueryService.GetPermissionConditionDetailAsync(result.ConditionId, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }

    #endregion PermissionCondition
}
