#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceQueryService
// Guid:c0d1e2f3-a4b5-4c5d-6e7f-9a0b1c2d3e4f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 资源查询服务（处理资源的读操作 - CQRS）
/// </summary>
public class ResourceQueryService : ApplicationServiceBase
{
    private readonly IResourceRepository _resourceRepository;
    private readonly AuthorizationDomainService _authorizationDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ResourceQueryService(
        IResourceRepository resourceRepository,
        AuthorizationDomainService authorizationDomainService)
    {
        _resourceRepository = resourceRepository;
        _authorizationDomainService = authorizationDomainService;
    }

    /// <summary>
    /// 根据ID获取资源
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <returns>资源DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var resource = await _resourceRepository.GetByIdAsync(id);
        return resource?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据资源编码获取资源
    /// </summary>
    /// <param name="resourceCode">资源编码</param>
    /// <returns>资源DTO</returns>
    public async Task<RbacDtoBase?> GetByResourceCodeAsync(string resourceCode)
    {
        var resource = await _resourceRepository.GetByResourceCodeAsync(resourceCode);
        return resource?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据资源类型获取资源列表
    /// </summary>
    /// <param name="resourceType">资源类型</param>
    /// <returns>资源DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByResourceTypeAsync(ResourceType resourceType)
    {
        var resources = await _resourceRepository.GetByResourceTypeAsync(resourceType);
        return resources.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据API路径查询资源
    /// </summary>
    /// <param name="apiPath">API路径</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <returns>资源DTO</returns>
    public async Task<RbacDtoBase?> GetByApiPathAsync(string apiPath, string? httpMethod = null)
    {
        var resource = await _resourceRepository.GetByApiPathAsync(apiPath, httpMethod);
        return resource?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户可访问的资源
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <returns>资源DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetUserAccessibleResourcesAsync(long userId, ResourceType? resourceType = null)
    {
        var resources = await _authorizationDomainService.GetUserAccessibleResourcesAsync(userId, resourceType);
        return resources.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色可访问的资源
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <returns>资源DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByRoleIdAsync(long roleId, ResourceType? resourceType = null)
    {
        var resources = await _resourceRepository.GetByRoleIdAsync(roleId, resourceType);
        return resources.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 批量获取资源
    /// </summary>
    /// <param name="resourceIds">资源ID列表</param>
    /// <returns>资源DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByIdsAsync(List<long> resourceIds)
    {
        var resources = await _resourceRepository.GetByIdsAsync(resourceIds);
        return resources.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 检查用户是否可以访问资源
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceId">资源ID</param>
    /// <returns>是否可以访问</returns>
    public async Task<bool> CanAccessResourceAsync(long userId, long resourceId)
    {
        return await _authorizationDomainService.CanAccessResourceAsync(userId, resourceId);
    }

    /// <summary>
    /// 检查用户是否可以访问API
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="apiPath">API路径</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <returns>是否可以访问</returns>
    public async Task<bool> CanAccessApiAsync(long userId, string apiPath, string? httpMethod = null)
    {
        return await _authorizationDomainService.CanAccessApiAsync(userId, apiPath, httpMethod);
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _resourceRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
