#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionQueryService
// Guid:e6f7a8b9-c0d1-4e5f-2a3b-5c6d7e8f9a0b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 权限查询服务（处理权限的读操作 - CQRS）
/// </summary>
public class PermissionQueryService : ApplicationServiceBase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly PermissionDomainService _permissionDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionQueryService(
        IPermissionRepository permissionRepository,
        PermissionDomainService permissionDomainService)
    {
        _permissionRepository = permissionRepository;
        _permissionDomainService = permissionDomainService;
    }

    /// <summary>
    /// 根据ID获取权限
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <returns>权限DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <returns>权限DTO</returns>
    public async Task<RbacDtoBase?> GetByPermissionCodeAsync(string permissionCode)
    {
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionCode);
        return permission?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByUserIdAsync(long userId)
    {
        var permissions = await _permissionDomainService.GetUserPermissionsAsync(userId);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByRoleIdAsync(long roleId)
    {
        var permissions = await _permissionRepository.GetByRoleIdAsync(roleId);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取资源的所有权限
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByResourceIdAsync(long resourceId)
    {
        var permissions = await _permissionDomainService.GetResourcePermissionsAsync(resourceId);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 批量获取权限
    /// </summary>
    /// <param name="permissionIds">权限ID列表</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByIdsAsync(List<long> permissionIds)
    {
        var permissions = await _permissionRepository.GetByIdsAsync(permissionIds);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据权限编码批量获取权限
    /// </summary>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByCodesAsync(List<string> permissionCodes)
    {
        var permissions = await _permissionRepository.GetByCodesAsync(permissionCodes);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _permissionRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限编码</param>
    /// <returns>是否拥有权限</returns>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode)
    {
        return await _permissionDomainService.HasPermissionAsync(userId, permissionCode);
    }

    /// <summary>
    /// 检查用户是否拥有任一权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <returns>是否拥有任一权限</returns>
    public async Task<bool> HasAnyPermissionAsync(long userId, List<string> permissionCodes)
    {
        return await _permissionDomainService.HasAnyPermissionAsync(userId, permissionCodes);
    }

    /// <summary>
    /// 检查用户是否拥有所有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <returns>是否拥有所有权限</returns>
    public async Task<bool> HasAllPermissionsAsync(long userId, List<string> permissionCodes)
    {
        return await _permissionDomainService.HasAllPermissionsAsync(userId, permissionCodes);
    }
}
