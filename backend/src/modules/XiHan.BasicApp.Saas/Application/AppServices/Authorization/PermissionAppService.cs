#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAppService
// Guid:c71b9028-19a3-4c87-9ad7-2f1211906dcc
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
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限定义")]
public sealed class PermissionAppService
    : SaasApplicationService, IPermissionAppService
{
    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 操作查询服务
    /// </summary>
    private readonly IOperationQueryService _operationQueryService;

    /// <summary>
    /// 权限目录领域服务
    /// </summary>
    private readonly IPermissionCatalogDomainService _permissionCatalogDomainService;

    /// <summary>
    /// 权限 ABAC 条件领域服务
    /// </summary>
    private readonly IPermissionConditionDomainService _permissionConditionDomainService;

    /// <summary>
    /// 权限 ABAC 条件查询服务
    /// </summary>
    private readonly IPermissionConditionQueryService _permissionConditionQueryService;

    /// <summary>
    /// 权限委托领域服务
    /// </summary>
    private readonly IPermissionDelegationDomainService _permissionDelegationDomainService;

    /// <summary>
    /// 权限委托查询服务
    /// </summary>
    private readonly IPermissionDelegationQueryService _permissionDelegationQueryService;

    /// <summary>
    /// 权限查询服务
    /// </summary>
    private readonly IPermissionQueryService _permissionQueryService;

    /// <summary>
    /// 权限申请领域服务
    /// </summary>
    private readonly IPermissionRequestDomainService _permissionRequestDomainService;

    /// <summary>
    /// 权限申请查询服务
    /// </summary>
    private readonly IPermissionRequestQueryService _permissionRequestQueryService;

    /// <summary>
    /// 资源查询服务
    /// </summary>
    private readonly IResourceQueryService _resourceQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionAppService(
        IPermissionCatalogDomainService permissionCatalogDomainService,
        IPermissionQueryService permissionQueryService,
        IResourceQueryService resourceQueryService,
        IOperationQueryService operationQueryService,
        IPermissionConditionDomainService permissionConditionDomainService,
        IPermissionConditionQueryService permissionConditionQueryService,
        IPermissionDelegationDomainService permissionDelegationDomainService,
        IPermissionDelegationQueryService permissionDelegationQueryService,
        IPermissionRequestDomainService permissionRequestDomainService,
        IPermissionRequestQueryService permissionRequestQueryService,
        ICurrentUser currentUser)
    {
        _permissionCatalogDomainService = permissionCatalogDomainService;
        _permissionQueryService = permissionQueryService;
        _resourceQueryService = resourceQueryService;
        _operationQueryService = operationQueryService;
        _permissionConditionDomainService = permissionConditionDomainService;
        _permissionConditionQueryService = permissionConditionQueryService;
        _permissionDelegationDomainService = permissionDelegationDomainService;
        _permissionDelegationQueryService = permissionDelegationQueryService;
        _permissionRequestDomainService = permissionRequestDomainService;
        _permissionRequestQueryService = permissionRequestQueryService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 创建权限定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Create)]
    public async Task<PermissionDetailDto> CreatePermissionAsync(PermissionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.CreatePermissionAsync(PermissionApplicationMapper.ToCreateCommand(input), cancellationToken);

        return await _permissionQueryService.GetPermissionDetailAsync(result.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
    }

    /// <summary>
    /// 删除权限定义
    /// </summary>
    /// <param name="id">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Delete)]
    public async Task DeletePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _permissionCatalogDomainService.DeletePermissionAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新权限定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Update)]
    public async Task<PermissionDetailDto> UpdatePermissionAsync(PermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdatePermissionAsync(PermissionApplicationMapper.ToUpdateCommand(input), cancellationToken);

        return await _permissionQueryService.GetPermissionDetailAsync(result.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
    }

    /// <summary>
    /// 更新权限定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Status)]
    public async Task<PermissionDetailDto> UpdatePermissionStatusAsync(PermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdatePermissionStatusAsync(PermissionApplicationMapper.ToStatusCommand(input), cancellationToken);

        return await _permissionQueryService.GetPermissionDetailAsync(result.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
    }

    #region Resource

    /// <summary>
    /// 创建资源定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Create)]
    public async Task<ResourceDetailDto> CreateResourceAsync(ResourceCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.CreateResourceAsync(ResourceApplicationMapper.ToCreateCommand(input), cancellationToken);

        return await _resourceQueryService.GetResourceDetailAsync(result.ResourceId, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");
    }

    /// <summary>
    /// 删除资源定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Delete)]
    public async Task DeleteResourceAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _permissionCatalogDomainService.DeleteResourceAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新资源定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Update)]
    public async Task<ResourceDetailDto> UpdateResourceAsync(ResourceUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdateResourceAsync(ResourceApplicationMapper.ToUpdateCommand(input), cancellationToken);

        return await _resourceQueryService.GetResourceDetailAsync(result.ResourceId, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");
    }

    /// <summary>
    /// 更新资源定义状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Status)]
    public async Task<ResourceDetailDto> UpdateResourceStatusAsync(ResourceStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdateResourceStatusAsync(ResourceApplicationMapper.ToStatusCommand(input), cancellationToken);

        return await _resourceQueryService.GetResourceDetailAsync(result.ResourceId, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");
    }

    #endregion Resource

    #region Operation

    /// <summary>
    /// 创建操作定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Create)]
    public async Task<OperationDetailDto> CreateOperationAsync(OperationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.CreateOperationAsync(OperationApplicationMapper.ToCreateCommand(input), cancellationToken);

        return await _operationQueryService.GetOperationDetailAsync(result.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");
    }

    /// <summary>
    /// 删除操作定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Delete)]
    public async Task DeleteOperationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _permissionCatalogDomainService.DeleteOperationAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新操作定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Update)]
    public async Task<OperationDetailDto> UpdateOperationAsync(OperationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdateOperationAsync(OperationApplicationMapper.ToUpdateCommand(input), cancellationToken);

        return await _operationQueryService.GetOperationDetailAsync(result.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");
    }

    /// <summary>
    /// 更新操作定义状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Status)]
    public async Task<OperationDetailDto> UpdateOperationStatusAsync(OperationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdateOperationStatusAsync(OperationApplicationMapper.ToStatusCommand(input), cancellationToken);

        return await _operationQueryService.GetOperationDetailAsync(result.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");
    }

    #endregion Operation

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

    #region PermissionDelegation

    /// <summary>
    /// 创建权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Create)]
    public async Task<PermissionDelegationDetailDto> CreatePermissionDelegationAsync(PermissionDelegationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.CreatePermissionDelegationAsync(PermissionDelegationApplicationMapper.ToCreateCommand(input), cancellationToken);

        return await _permissionDelegationQueryService.GetPermissionDelegationDetailAsync(result.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    /// <summary>
    /// 撤销权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Revoke)]
    public async Task DeletePermissionDelegationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _permissionDelegationDomainService.RevokePermissionDelegationAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Update)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationAsync(PermissionDelegationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.UpdatePermissionDelegationAsync(PermissionDelegationApplicationMapper.ToUpdateCommand(input), cancellationToken);

        return await _permissionDelegationQueryService.GetPermissionDelegationDetailAsync(result.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    /// <summary>
    /// 更新权限委托状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Status)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.UpdatePermissionDelegationStatusAsync(PermissionDelegationApplicationMapper.ToStatusCommand(input), cancellationToken);

        return await _permissionDelegationQueryService.GetPermissionDelegationDetailAsync(result.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    #endregion PermissionDelegation

    #region PermissionRequest

    /// <summary>
    /// 创建权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Create)]
    public async Task<PermissionRequestDetailDto> CreatePermissionRequestAsync(PermissionRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        var result = await _permissionRequestDomainService.CreatePermissionRequestAsync(
            PermissionRequestApplicationMapper.ToCreateCommand(input, requestUserId),
            cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 撤回权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Withdraw)]
    public async Task DeletePermissionRequestAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        await _permissionRequestDomainService.WithdrawPermissionRequestAsync(id, requestUserId, cancellationToken);
    }

    /// <summary>
    /// 更新权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Update)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestAsync(PermissionRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        var result = await _permissionRequestDomainService.UpdatePermissionRequestAsync(
            PermissionRequestApplicationMapper.ToUpdateCommand(input, requestUserId),
            cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 更新权限申请状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestStatusAsync(PermissionRequestStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionRequestDomainService.UpdatePermissionRequestStatusAsync(
            PermissionRequestApplicationMapper.ToStatusCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 获取当前用户主键
    /// </summary>
    private long GetCurrentUserIdOrThrow()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("当前用户未登录。");
        }

        return _currentUser.UserId.Value;
    }

    #endregion PermissionRequest
}
