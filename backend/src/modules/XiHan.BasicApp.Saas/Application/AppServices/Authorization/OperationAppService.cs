// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
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
/// 操作定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "操作定义")]
public sealed class OperationAppService
    : SaasApplicationService, IOperationAppService
{
    /// <summary>
    /// 操作查询服务
    /// </summary>
    private readonly IOperationQueryService _operationQueryService;

    /// <summary>
    /// 权限目录领域服务
    /// </summary>
    private readonly IPermissionCatalogDomainService _permissionCatalogDomainService;

    /// <summary>
    /// 缓存失效器
    /// </summary>
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationAppService(
        IPermissionCatalogDomainService permissionCatalogDomainService,
        IOperationQueryService operationQueryService,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _permissionCatalogDomainService = permissionCatalogDomainService;
        _operationQueryService = operationQueryService;
        _cacheInvalidator = cacheInvalidator;
    }

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

        // 操作定义变更影响可选操作选择项缓存
        await _cacheInvalidator.InvalidateOperationDefinitionAsync(cancellationToken);

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

        // 操作定义删除影响可选操作选择项缓存
        await _cacheInvalidator.InvalidateOperationDefinitionAsync(cancellationToken);
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

        // 操作定义变更影响可选操作选择项缓存
        await _cacheInvalidator.InvalidateOperationDefinitionAsync(cancellationToken);

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

        // 操作定义启停影响可选操作选择项缓存
        await _cacheInvalidator.InvalidateOperationDefinitionAsync(cancellationToken);

        return await _operationQueryService.GetOperationDetailAsync(result.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");
    }

    #endregion Operation
}
