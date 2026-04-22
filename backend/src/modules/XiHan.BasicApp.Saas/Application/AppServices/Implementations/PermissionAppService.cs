#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAppService
// Guid:467e4fc7-d206-418c-bf77-5b2e1cd252ec
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 权限应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("permission:read")]
public class PermissionAppService
    : CrudApplicationServiceBase<SysPermission, PermissionDto, long, PermissionCreateDto, PermissionUpdateDto, BasicAppPRDto>,
        IPermissionAppService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionQueryService _queryService;
    private readonly IPermissionDomainService _domainService;
    private readonly IRbacChangeNotifier _rbacChangeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionRepository"></param>
    /// <param name="queryService"></param>
    /// <param name="domainService"></param>
    /// <param name="rbacChangeNotifier"></param>
    public PermissionAppService(
        IPermissionRepository permissionRepository,
        IPermissionQueryService queryService,
        IPermissionDomainService domainService,
        IRbacChangeNotifier rbacChangeNotifier)
        : base(permissionRepository)
    {
        _permissionRepository = permissionRepository;
        _queryService = queryService;
        _domainService = domainService;
        _rbacChangeNotifier = rbacChangeNotifier;
    }

    /// <summary>
    /// 根据ID获取权限
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("permission:read")]
    public override async Task<PermissionDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("permission:read")]
    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
        }

        return await _queryService.GetRolePermissionsAsync(roleId, tenantId);
    }

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [PermissionAuthorize("permission:read")]
    public async Task<IReadOnlyList<PermissionDto>> GetUserPermissionsAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query.UserId));
        }

        return await _queryService.GetUserPermissionsAsync(query.UserId, query.TenantId);
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("permission:create")]
    public override async Task<PermissionDto> CreateAsync(PermissionCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.PermissionCode.Trim();
        await EnsurePermissionCodeUniqueAsync(normalizedCode, null, input.TenantId);

        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        await _rbacChangeNotifier.NotifyAsync(created.TenantId, AuthorizationChangeType.Permission);
        return MapPermission(created);
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("permission:update")]
    public override async Task<PermissionDto> UpdateAsync(PermissionUpdateDto input)
    {
        input.ValidateAnnotations();

        var permission = await _permissionRepository.GetByIdAsync(input.BasicId)
                         ?? throw new KeyNotFoundException($"未找到权限: {input.BasicId}");

        var normalizedCode = input.PermissionCode.Trim();
        await EnsurePermissionCodeUniqueAsync(normalizedCode, input.BasicId, permission.TenantId);

        await MapDtoToEntityAsync(input, permission);
        var updated = await _domainService.UpdateAsync(permission);
        await _rbacChangeNotifier.NotifyAsync(updated.TenantId, AuthorizationChangeType.Permission);
        return MapPermission(updated);
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("permission:delete")]
    public override async Task<bool> DeleteAsync(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("权限 ID 无效", nameof(id));
        }

        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission is null)
        {
            return false;
        }

        var deleted = await _domainService.DeleteAsync(id);
        if (deleted)
        {
            await _rbacChangeNotifier.NotifyAsync(permission.TenantId, AuthorizationChangeType.Permission);
        }

        return deleted;
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    /// <param name="createDto"></param>
    /// <returns></returns>
    protected override Task<SysPermission> MapDtoToEntityAsync(PermissionCreateDto createDto)
    {
        var entity = new SysPermission
        {
            TenantId = createDto.TenantId ?? 0,
            ResourceId = createDto.ResourceId,
            OperationId = createDto.OperationId,
            PermissionCode = createDto.PermissionCode.Trim(),
            PermissionName = createDto.PermissionName.Trim(),
            PermissionDescription = createDto.PermissionDescription,
            Tags = NormalizeNullable(createDto.Tags),
            IsRequireAudit = createDto.IsRequireAudit,
            IsGlobal = createDto.IsGlobal,
            Priority = createDto.Priority,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    /// <param name="updateDto"></param>
    /// <param name="entity"></param>
    protected override Task MapDtoToEntityAsync(PermissionUpdateDto updateDto, SysPermission entity)
    {
        entity.ResourceId = updateDto.ResourceId;
        entity.OperationId = updateDto.OperationId;
        entity.PermissionCode = updateDto.PermissionCode.Trim();
        entity.PermissionName = updateDto.PermissionName.Trim();
        entity.PermissionDescription = updateDto.PermissionDescription;
        entity.Tags = NormalizeNullable(updateDto.Tags) ?? entity.Tags;
        entity.IsRequireAudit = updateDto.IsRequireAudit;
        entity.IsGlobal = updateDto.IsGlobal;
        entity.Priority = updateDto.Priority;
        entity.Status = updateDto.Status;
        entity.Sort = updateDto.Sort;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射实体到 DTO，补齐前端展示字段。
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected override Task<PermissionDto> MapEntityToDtoAsync(SysPermission entity)
    {
        return Task.FromResult(MapPermission(entity));
    }

    /// <summary>
    /// 批量映射实体到 DTO。
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    protected override Task<IList<PermissionDto>> MapEntitiesToDtosAsync(IEnumerable<SysPermission> entities)
    {
        return Task.FromResult<IList<PermissionDto>>(entities.Select(MapPermission).ToArray());
    }

    /// <summary>
    /// 校验权限编码唯一性
    /// </summary>
    /// <param name="permissionCode"></param>
    /// <param name="excludePermissionId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsurePermissionCodeUniqueAsync(string permissionCode, long? excludePermissionId, long? tenantId)
    {
        var existing = await _permissionRepository.GetByPermissionCodeAsync(permissionCode, tenantId);
        if (existing is not null && (!excludePermissionId.HasValue || existing.BasicId != excludePermissionId.Value))
        {
            throw new BusinessException(message: $"权限编码 '{permissionCode}' 已存在");
        }
    }

    private static PermissionDto MapPermission(SysPermission permission)
    {
        return SaasReadModelMapper.MapPermission(permission);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

}
