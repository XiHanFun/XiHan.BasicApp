#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleAppService
// Guid:bc22b1f0-ff83-43ee-864e-2beb29117713
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
/// 角色命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色")]
public sealed class RoleAppService(
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IRolePermissionRepository rolePermissionRepository,
    IRoleHierarchyRepository roleHierarchyRepository,
    IRoleDataScopeRepository roleDataScopeRepository)
    : SaasApplicationService, IRoleAppService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 用户角色仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    /// <summary>
    /// 角色层级仓储
    /// </summary>
    private readonly IRoleHierarchyRepository _roleHierarchyRepository = roleHierarchyRepository;

    /// <summary>
    /// 角色数据范围仓储
    /// </summary>
    private readonly IRoleDataScopeRepository _roleDataScopeRepository = roleDataScopeRepository;

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Create)]
    public async Task<RoleDetailDto> CreateRoleAsync(RoleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var roleCode = input.RoleCode.Trim();
        if (await _roleRepository.GetByCodeAsync(roleCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("角色编码已存在。");
        }

        var role = new SysRole
        {
            RoleCode = roleCode,
            RoleName = input.RoleName.Trim(),
            RoleDescription = NormalizeNullable(input.RoleDescription),
            RoleType = input.RoleType,
            IsGlobal = false,
            DataScope = input.DataScope,
            MaxMembers = input.MaxMembers,
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedRole = await _roleRepository.AddAsync(role, cancellationToken);
        return RoleApplicationMapper.ToDetailDto(savedRole);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Update)]
    public async Task<RoleDetailDto> UpdateRoleAsync(RoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var role = await GetEditableRoleOrThrowAsync(input.BasicId, cancellationToken);
        role.RoleName = input.RoleName.Trim();
        role.RoleDescription = NormalizeNullable(input.RoleDescription);
        role.RoleType = input.RoleType;
        role.DataScope = input.DataScope;
        role.MaxMembers = input.MaxMembers;
        role.Sort = input.Sort;
        role.Remark = NormalizeNullable(input.Remark);

        var savedRole = await _roleRepository.UpdateAsync(role, cancellationToken);
        return RoleApplicationMapper.ToDetailDto(savedRole);
    }

    /// <summary>
    /// 更新角色状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Status)]
    public async Task<RoleDetailDto> UpdateRoleStatusAsync(RoleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var role = await GetEditableRoleOrThrowAsync(input.BasicId, cancellationToken);
        role.Status = input.Status;
        role.Remark = NormalizeNullable(input.Remark) ?? role.Remark;

        var savedRole = await _roleRepository.UpdateAsync(role, cancellationToken);
        return RoleApplicationMapper.ToDetailDto(savedRole);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="id">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Delete)]
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await GetEditableRoleOrThrowAsync(id, cancellationToken);
        await EnsureRoleNotReferencedAsync(role.BasicId, cancellationToken);

        if (!await _roleRepository.DeleteAsync(role, cancellationToken))
        {
            throw new InvalidOperationException("角色删除失败。");
        }
    }

    /// <summary>
    /// 获取可维护角色，不满足规则时抛出异常
    /// </summary>
    /// <param name="id">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    private async Task<SysRole> GetEditableRoleOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色主键必须大于 0。");
        }

        var role = await _roleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        EnsureRoleCanBeMaintained(role);
        return role;
    }

    /// <summary>
    /// 校验角色未被其它授权事实引用
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureRoleNotReferencedAsync(long roleId, CancellationToken cancellationToken)
    {
        if (await _userRoleRepository.AnyAsync(userRole => userRole.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已分配给用户，不能删除。");
        }

        if (await _rolePermissionRepository.AnyAsync(rolePermission => rolePermission.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已绑定权限，不能删除。");
        }

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.Depth > 0 && (hierarchy.AncestorId == roleId || hierarchy.DescendantId == roleId),
            cancellationToken))
        {
            throw new InvalidOperationException("角色存在继承关系，不能删除。");
        }

        if (await _roleDataScopeRepository.AnyAsync(dataScope => dataScope.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已配置数据范围，不能删除。");
        }
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    /// <param name="input">创建参数</param>
    private static void ValidateCreateInput(RoleCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.RoleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.RoleName);
        ValidateCommonInput(input.RoleType, input.DataScope, input.MaxMembers);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(RoleUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(input.RoleName);
        ValidateCommonInput(input.RoleType, input.DataScope, input.MaxMembers);
    }

    /// <summary>
    /// 校验角色通用资料
    /// </summary>
    /// <param name="roleType">角色类型</param>
    /// <param name="dataScope">数据权限范围</param>
    /// <param name="maxMembers">最大成员数</param>
    private static void ValidateCommonInput(RoleType roleType, DataPermissionScope dataScope, int maxMembers)
    {
        ValidateEnum(roleType, nameof(roleType));
        ValidateEnum(dataScope, nameof(dataScope));

        if (roleType == RoleType.System)
        {
            throw new InvalidOperationException("系统角色必须通过平台种子或运维流程维护。");
        }

        if (maxMembers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMembers), "最大成员数不能小于 0。");
        }
    }

    /// <summary>
    /// 校验角色是否允许通过本服务维护
    /// </summary>
    /// <param name="role">角色实体</param>
    private static void EnsureRoleCanBeMaintained(SysRole role)
    {
        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护。");
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
