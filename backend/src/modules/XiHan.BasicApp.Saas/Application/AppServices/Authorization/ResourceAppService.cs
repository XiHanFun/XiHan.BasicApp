#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceAppService
// Guid:5a1c8e3b-2f4d-4a6b-9c7d-1e8f0a2b3c4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 资源定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "资源定义")]
public sealed class ResourceAppService
    : SaasApplicationService, IResourceAppService
{
    /// <summary>
    /// 权限目录领域服务
    /// </summary>
    private readonly IPermissionCatalogDomainService _permissionCatalogDomainService;

    /// <summary>
    /// 资源查询服务
    /// </summary>
    private readonly IResourceQueryService _resourceQueryService;

    /// <summary>
    /// 缓存失效器
    /// </summary>
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ResourceAppService(
        IPermissionCatalogDomainService permissionCatalogDomainService,
        IResourceQueryService resourceQueryService,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _permissionCatalogDomainService = permissionCatalogDomainService;
        _resourceQueryService = resourceQueryService;
        _cacheInvalidator = cacheInvalidator;
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

        // 资源定义变更影响可选资源选择项缓存
        await _cacheInvalidator.InvalidateResourceDefinitionAsync(cancellationToken);

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

        // 资源定义删除影响可选资源选择项缓存
        await _cacheInvalidator.InvalidateResourceDefinitionAsync(cancellationToken);
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

        // 资源定义变更影响可选资源选择项缓存
        await _cacheInvalidator.InvalidateResourceDefinitionAsync(cancellationToken);

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

        // 资源定义启停影响可选资源选择项缓存
        await _cacheInvalidator.InvalidateResourceDefinitionAsync(cancellationToken);

        return await _resourceQueryService.GetResourceDetailAsync(result.ResourceId, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");
    }

    #endregion Resource

}
