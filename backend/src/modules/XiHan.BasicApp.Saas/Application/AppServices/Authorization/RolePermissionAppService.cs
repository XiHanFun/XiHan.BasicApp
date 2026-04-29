#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionAppService
// Guid:4f471ac1-0b53-4c54-a207-9a45b09ebc2e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 角色权限命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色权限")]
public sealed class RolePermissionAppService(
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository,
    IPermissionRepository permissionRepository)
    : SaasApplicationService, IRolePermissionAppService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 授予角色权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Grant)]
    public async Task<RolePermissionDetailDto> CreateRolePermissionAsync(RolePermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        _ = await GetEnabledRoleOrThrowAsync(input.RoleId, cancellationToken);
        var permission = await GetEnabledPermissionOrThrowAsync(input.PermissionId, cancellationToken);
        if (await _rolePermissionRepository.AnyAsync(
            rolePermission => rolePermission.RoleId == input.RoleId && rolePermission.PermissionId == input.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色权限已绑定。");
        }

        var rolePermission = new SysRolePermission
        {
            RoleId = input.RoleId,
            PermissionId = input.PermissionId,
            PermissionAction = input.PermissionAction,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            GrantReason = NormalizeNullable(input.GrantReason),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedRolePermission = await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(savedRolePermission, permission);
    }

    /// <summary>
    /// 更新角色权限
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Update)]
    public async Task<RolePermissionDetailDto> UpdateRolePermissionAsync(RolePermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var rolePermission = await GetRolePermissionOrThrowAsync(input.BasicId, cancellationToken);
        _ = await GetEnabledRoleOrThrowAsync(rolePermission.RoleId, cancellationToken);
        var permission = await GetEnabledPermissionOrThrowAsync(rolePermission.PermissionId, cancellationToken);

        rolePermission.PermissionAction = input.PermissionAction;
        rolePermission.EffectiveTime = input.EffectiveTime;
        rolePermission.ExpirationTime = input.ExpirationTime;
        rolePermission.GrantReason = NormalizeNullable(input.GrantReason);
        rolePermission.Remark = NormalizeNullable(input.Remark);

        var savedRolePermission = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(savedRolePermission, permission);
    }

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Status)]
    public async Task<RolePermissionDetailDto> UpdateRolePermissionStatusAsync(RolePermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var rolePermission = await GetRolePermissionOrThrowAsync(input.BasicId, cancellationToken);
        var permission = input.Status == ValidityStatus.Valid
            ? await GetEnabledPermissionOrThrowAsync(rolePermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);

        if (input.Status == ValidityStatus.Valid)
        {
            _ = await GetEnabledRoleOrThrowAsync(rolePermission.RoleId, cancellationToken);
        }

        rolePermission.Status = input.Status;
        rolePermission.Remark = NormalizeNullable(input.Remark);

        var savedRolePermission = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(savedRolePermission, permission);
    }

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    /// <param name="id">角色权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Revoke)]
    public async Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rolePermission = await GetRolePermissionOrThrowAsync(id, cancellationToken);
        rolePermission.Status = ValidityStatus.Invalid;

        _ = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken);
    }

    /// <summary>
    /// 获取角色权限绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">角色权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限绑定实体</returns>
    private async Task<SysRolePermission> GetRolePermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色权限绑定主键必须大于 0。");
        }

        return await _rolePermissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色权限绑定不存在。");
    }

    /// <summary>
    /// 获取已启用角色，不满足规则时抛出异常
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    private async Task<SysRole> GetEnabledRoleOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能维护权限绑定。");
        }

        return role;
    }

    /// <summary>
    /// 获取已启用权限，不满足规则时抛出异常
    /// </summary>
    /// <param name="permissionId">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    private async Task<SysPermission> GetEnabledPermissionOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能绑定到角色。");
        }

        return permission;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(RolePermissionGrantDto input)
    {
        if (input.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        if (input.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限主键必须大于 0。");
        }

        ValidateEnum(input.PermissionAction, nameof(input.PermissionAction));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(RolePermissionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.PermissionAction, nameof(input.PermissionAction));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验有效期
    /// </summary>
    /// <param name="effectiveTime">生效时间</param>
    /// <param name="expirationTime">失效时间</param>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("角色权限失效时间必须晚于生效时间。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="paramName">参数名</param>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
