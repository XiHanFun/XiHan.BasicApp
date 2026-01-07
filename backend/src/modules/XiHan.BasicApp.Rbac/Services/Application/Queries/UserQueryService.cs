#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserQueryService
// Guid:b3c4d5e6-f7a8-4b5c-0d1e-2f3a4b5c6d7e
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
/// 用户查询服务（处理用户的读操作 - CQRS）
/// </summary>
public class UserQueryService : ApplicationServiceBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly PermissionDomainService _permissionDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserQueryService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        PermissionDomainService permissionDomainService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _permissionDomainService = permissionDomainService;
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>用户DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>用户DTO</returns>
    public async Task<RbacDtoBase?> GetByUserNameAsync(string userName)
    {
        var user = await _userRepository.GetByUserNameAsync(userName);
        return user?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>用户DTO</returns>
    public async Task<RbacDtoBase?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns>用户DTO</returns>
    public async Task<RbacDtoBase?> GetByPhoneAsync(string phone)
    {
        var user = await _userRepository.GetByPhoneAsync(phone);
        return user?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户及其角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户DTO（包含角色）</returns>
    public async Task<RbacDtoBase?> GetUserWithRolesAsync(long userId)
    {
        var user = await _userRepository.GetWithRolesAsync(userId);
        return user?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>角色DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetUserRolesAsync(long userId)
    {
        // 需要通过关系表查询（实际实现需要根据具体的关系表处理）
        // 这里只是示例结构
        return new List<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetUserPermissionsAsync(long userId)
    {
        var permissions = await _permissionDomainService.GetUserPermissionsAsync(userId);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取用户的权限编码列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限编码列表</returns>
    public async Task<List<string>> GetUserPermissionCodesAsync(long userId)
    {
        return await _permissionDomainService.GetUserPermissionCodesAsync(userId);
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
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _userRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }

    /// <summary>
    /// 获取租户下的所有用户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>用户DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByTenantIdAsync(long tenantId)
    {
        var users = await _userRepository.GetByTenantIdAsync(tenantId);
        return users.Adapt<List<RbacDtoBase>>();
    }
}
