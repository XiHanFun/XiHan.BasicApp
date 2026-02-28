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
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 权限应用服务
/// </summary>
public class PermissionAppService
    : CrudApplicationServiceBase<SysPermission, PermissionDto, long, PermissionCreateDto, PermissionUpdateDto, BasicAppPRDto>,
        IPermissionAppService
{
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionRepository"></param>
    public PermissionAppService(IPermissionRepository permissionRepository)
        : base(permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        var permissions = await _permissionRepository.GetRolePermissionsAsync(roleId, tenantId);
        return permissions.Select(static permission => permission.Adapt<PermissionDto>()).ToArray();
    }

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<PermissionDto>> GetUserPermissionsAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var permissions = await _permissionRepository.GetUserPermissionsAsync(query.UserId, query.TenantId);
        return permissions.Select(static permission => permission.Adapt<PermissionDto>()).ToArray();
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<PermissionDto> CreateAsync(PermissionCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.PermissionCode.Trim();
        await EnsurePermissionCodeUniqueAsync(normalizedCode, null, input.TenantId);

        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<PermissionDto> UpdateAsync(long id, PermissionUpdateDto input)
    {
        input.ValidateAnnotations();

        var permission = await _permissionRepository.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"未找到权限: {id}");

        var normalizedCode = input.PermissionCode.Trim();
        await EnsurePermissionCodeUniqueAsync(normalizedCode, id, permission.TenantId);

        await MapDtoToEntityAsync(input, permission);
        var updated = await _permissionRepository.UpdateAsync(permission);
        return updated.Adapt<PermissionDto>();
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
            TenantId = createDto.TenantId,
            ResourceId = createDto.ResourceId,
            OperationId = createDto.OperationId,
            PermissionCode = createDto.PermissionCode.Trim(),
            PermissionName = createDto.PermissionName.Trim(),
            PermissionDescription = createDto.PermissionDescription,
            IsRequireAudit = createDto.IsRequireAudit,
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
        entity.IsRequireAudit = updateDto.IsRequireAudit;
        entity.Priority = updateDto.Priority;
        entity.Status = updateDto.Status;
        entity.Sort = updateDto.Sort;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
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
            throw new InvalidOperationException($"权限编码 '{permissionCode}' 已存在");
        }
    }
}
