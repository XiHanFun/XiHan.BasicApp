#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserQueryService
// Guid:3a4b5c6d-7e8f-4012-cdef-320000000002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户查询服务
/// </summary>
public class UserQueryService : IUserQueryService, ITransientDependency
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantAccessContextService _tenantAccessContextService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserQueryService(IUserRepository userRepository, ITenantAccessContextService tenantAccessContextService)
    {
        _userRepository = userRepository;
        _tenantAccessContextService = tenantAccessContextService;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.UserById, ExpireSeconds = 300)]
    public async Task<UserDto?> GetByIdAsync(long id)
    {
        var entity = await _userRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return null;
        }

        var currentContext = await _tenantAccessContextService.GetCurrentContextAsync();
        return await MapUserToDtoAsync(entity, currentContext?.CurrentTenantId ?? (entity.TenantId > 0 ? entity.TenantId : null));
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByUserNameAsync(string userName, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var entity = await _userRepository.GetByUserNameAsync(userName, tenantId);
        return entity is null ? null : await MapUserToDtoAsync(entity, tenantId ?? entity.TenantId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRoleRelationDto>> GetUserRolesAsync(long userId, long? tenantId = null)
    {
        var relations = await _userRepository.GetUserRolesAsync(userId, tenantId);
        return relations.Select(relation => new UserRoleRelationDto
        {
            TenantId = relation.TenantId,
            UserId = relation.UserId,
            RoleId = relation.RoleId,
            Status = relation.Status
        }).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserPermissionRelationDto>> GetUserPermissionsAsync(long userId, long? tenantId = null)
    {
        var relations = await _userRepository.GetUserPermissionsAsync(userId, tenantId);
        return relations.Select(relation => new UserPermissionRelationDto
        {
            TenantId = relation.TenantId,
            UserId = relation.UserId,
            PermissionId = relation.PermissionId,
            PermissionAction = relation.PermissionAction,
            Status = relation.Status
        }).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserDepartmentRelationDto>> GetUserDepartmentsAsync(long userId, long? tenantId = null)
    {
        var relations = await _userRepository.GetUserDepartmentsAsync(userId, tenantId);
        return relations.Select(relation => new UserDepartmentRelationDto
        {
            TenantId = relation.TenantId,
            UserId = relation.UserId,
            DepartmentId = relation.DepartmentId,
            IsMain = relation.IsMain,
            Status = relation.Status
        }).ToArray();
    }

    private async Task<UserDto> MapUserToDtoAsync(SysUser entity, long? tenantId)
    {
        var dto = entity.Adapt<UserDto>()!;
        var relations = await _userRepository.GetUserRolesAsync(entity.BasicId, tenantId);
        dto.AccessibleTenantIds = await _userRepository.GetAccessibleTenantIdsAsync(entity.BasicId);
        dto.RoleIds = relations
            .Select(relation => relation.RoleId)
            .Distinct()
            .ToArray();
        return dto;
    }
}
