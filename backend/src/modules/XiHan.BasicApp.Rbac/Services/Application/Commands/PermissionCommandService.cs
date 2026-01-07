#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionCommandService
// Guid:d5e6f7a8-b9c0-4d5e-1f2a-4b5c6d7e8f9a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 权限命令服务（处理权限的写操作）
/// </summary>
public class PermissionCommandService : CrudApplicationServiceBase<SysPermission, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly PermissionDomainService _permissionDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionCommandService(
        IPermissionRepository permissionRepository,
        PermissionDomainService permissionDomainService)
        : base(permissionRepository)
    {
        _permissionRepository = permissionRepository;
        _permissionDomainService = permissionDomainService;
    }

    /// <summary>
    /// 创建权限（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        if (!await _permissionDomainService.IsPermissionCodeUniqueAsync(input.PermissionCode))
        {
            throw new InvalidOperationException($"权限编码 {input.PermissionCode} 已存在");
        }

        // 2. 映射并创建
        var permission = input.Adapt<SysPermission>();

        // 3. 保存
        permission = await _permissionRepository.AddAsync(permission);

        return await MapToEntityDtoAsync(permission);
    }

    /// <summary>
    /// 更新权限（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取权限
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new KeyNotFoundException($"权限 {id} 不存在");
        }

        // 2. 业务验证
        if (permission.PermissionCode != input.PermissionCode &&
            !await _permissionDomainService.IsPermissionCodeUniqueAsync(input.PermissionCode, id))
        {
            throw new InvalidOperationException($"权限编码 {input.PermissionCode} 已存在");
        }

        // 3. 更新实体
        input.Adapt(permission);

        // 4. 保存
        permission = await _permissionRepository.UpdateAsync(permission);

        return await MapToEntityDtoAsync(permission);
    }

    /// <summary>
    /// 批量创建权限
    /// </summary>
    /// <param name="inputs">权限DTO列表</param>
    /// <returns>创建的权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> BatchCreateAsync(List<RbacDtoBase> inputs)
    {
        // 1. 验证权限编码唯一性
        var codes = inputs.Select(x => x.PermissionCode).ToList();
        var existingPermissions = await _permissionRepository.GetByCodesAsync(codes);
        if (existingPermissions.Any())
        {
            var existingCodes = string.Join(", ", existingPermissions.Select(x => x.PermissionCode));
            throw new InvalidOperationException($"以下权限编码已存在: {existingCodes}");
        }

        // 2. 映射并批量创建
        var permissions = inputs.Adapt<List<SysPermission>>();
        permissions = await _permissionRepository.AddRangeAsync(permissions);

        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 更新权限状态
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long permissionId, Enums.YesOrNo status)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId);
        if (permission == null)
        {
            throw new KeyNotFoundException($"权限 {permissionId} 不存在");
        }

        permission.Status = status;
        await _permissionRepository.UpdateAsync(permission);
        return true;
    }
}
