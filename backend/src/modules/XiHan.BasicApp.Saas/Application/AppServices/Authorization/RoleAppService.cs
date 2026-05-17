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
public sealed class RoleAppService
    : SaasApplicationService, IRoleAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleAppService(
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IRoleDataScopeRepository roleDataScopeRepository,
        IPermissionRepository permissionRepository,
        IDepartmentRepository departmentRepository)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 用户角色仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository;

    /// <summary>
    /// 角色层级仓储
    /// </summary>
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 角色数据范围仓储
    /// </summary>
    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository;

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

    #region RolePermission

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

        ValidateRolePermissionGrantInput(input);

        _ = await GetEnabledRoleForPermissionOrThrowAsync(input.RoleId, cancellationToken);
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

        ValidateRolePermissionUpdateInput(input);

        var rolePermission = await GetRolePermissionOrThrowAsync(input.BasicId, cancellationToken);
        _ = await GetEnabledRoleForPermissionOrThrowAsync(rolePermission.RoleId, cancellationToken);
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
            _ = await GetEnabledRoleForPermissionOrThrowAsync(rolePermission.RoleId, cancellationToken);
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
    /// 获取已启用角色（权限绑定场景），不满足规则时抛出异常
    /// </summary>
    private async Task<SysRole> GetEnabledRoleForPermissionOrThrowAsync(long roleId, CancellationToken cancellationToken)
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
    /// 校验角色权限授权参数
    /// </summary>
    private static void ValidateRolePermissionGrantInput(RolePermissionGrantDto input)
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
    /// 校验角色权限更新参数
    /// </summary>
    private static void ValidateRolePermissionUpdateInput(RolePermissionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.PermissionAction, nameof(input.PermissionAction));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    #endregion RolePermission

    #region RoleDataScope

    /// <summary>
    /// 授予角色数据范围
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Grant)]
    public async Task<RoleDataScopeDetailDto> CreateRoleDataScopeAsync(RoleDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRoleDataScopeGrantInput(input);

        _ = await GetCustomDataScopeRoleOrThrowAsync(input.RoleId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(input.DepartmentId, cancellationToken);
        if (await _roleDataScopeRepository.AnyAsync(
            scope => scope.RoleId == input.RoleId && scope.DepartmentId == input.DepartmentId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色数据范围已绑定。");
        }

        var dataScope = new SysRoleDataScope
        {
            RoleId = input.RoleId,
            DepartmentId = input.DepartmentId,
            IncludeChildren = input.IncludeChildren,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDataScope = await _roleDataScopeRepository.AddAsync(dataScope, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(savedDataScope, department);
    }

    /// <summary>
    /// 更新角色数据范围
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Update)]
    public async Task<RoleDataScopeDetailDto> UpdateRoleDataScopeAsync(RoleDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRoleDataScopeUpdateInput(input);

        var dataScope = await GetRoleDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        _ = await GetCustomDataScopeRoleOrThrowAsync(dataScope.RoleId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken);

        dataScope.IncludeChildren = input.IncludeChildren;
        dataScope.EffectiveTime = input.EffectiveTime;
        dataScope.ExpirationTime = input.ExpirationTime;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(savedDataScope, department);
    }

    /// <summary>
    /// 更新角色数据范围状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Status)]
    public async Task<RoleDataScopeDetailDto> UpdateRoleDataScopeStatusAsync(RoleDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var dataScope = await GetRoleDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        var department = input.Status == ValidityStatus.Valid
            ? await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken)
            : await _departmentRepository.GetByIdAsync(dataScope.DepartmentId, cancellationToken);

        if (input.Status == ValidityStatus.Valid)
        {
            _ = await GetCustomDataScopeRoleOrThrowAsync(dataScope.RoleId, cancellationToken);
        }

        dataScope.Status = input.Status;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(savedDataScope, department);
    }

    /// <summary>
    /// 撤销角色数据范围
    /// </summary>
    /// <param name="id">角色数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Revoke)]
    public async Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await GetRoleDataScopeOrThrowAsync(id, cancellationToken);
        dataScope.Status = ValidityStatus.Invalid;

        _ = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
    }

    /// <summary>
    /// 获取角色数据范围绑定，不存在时抛出异常
    /// </summary>
    private async Task<SysRoleDataScope> GetRoleDataScopeOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色数据范围绑定主键必须大于 0。");
        }

        return await _roleDataScopeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色数据范围绑定不存在。");
    }

    /// <summary>
    /// 获取可维护自定义数据范围角色，不满足规则时抛出异常
    /// </summary>
    private async Task<SysRole> GetCustomDataScopeRoleOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能维护数据范围。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护。");
        }

        if (role.DataScope != DataPermissionScope.Custom)
        {
            throw new InvalidOperationException("只有自定义数据权限范围角色才能维护数据范围。");
        }

        return role;
    }

    /// <summary>
    /// 获取已启用部门，不满足规则时抛出异常
    /// </summary>
    private async Task<SysDepartment> GetEnabledDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能绑定到角色数据范围。");
        }

        return department;
    }

    /// <summary>
    /// 校验角色数据范围授权参数
    /// </summary>
    private static void ValidateRoleDataScopeGrantInput(RoleDataScopeGrantDto input)
    {
        if (input.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        if (input.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "部门主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验角色数据范围更新参数
    /// </summary>
    private static void ValidateRoleDataScopeUpdateInput(RoleDataScopeUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色数据范围绑定主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    #endregion RoleDataScope

    #region RoleHierarchy

    /// <summary>
    /// 创建角色直接继承关系
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Create)]
    public async Task<RoleHierarchyDetailDto> CreateRoleHierarchyAsync(RoleHierarchyCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRoleHierarchyCreateInput(input);

        var ancestor = await GetRoleForHierarchyOrThrowAsync(input.AncestorId, cancellationToken);
        var descendant = await GetRoleForHierarchyOrThrowAsync(input.DescendantId, cancellationToken);
        EnsureDescendantCanBeMaintainedForHierarchy(descendant);

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.AncestorId == input.AncestorId && hierarchy.DescendantId == input.DescendantId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色继承关系已存在。");
        }

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.AncestorId == input.DescendantId && hierarchy.DescendantId == input.AncestorId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色继承关系会形成环路。");
        }

        var existingHierarchies = (await _roleHierarchyRepository.GetAllAsync(cancellationToken)).ToList();
        var addList = new List<SysRoleHierarchy>();
        var workingHierarchies = new List<SysRoleHierarchy>(existingHierarchies);

        EnsureSelfHierarchy(ancestor, workingHierarchies, addList);
        EnsureSelfHierarchy(descendant, workingHierarchies, addList);

        var ancestorClosures = workingHierarchies
            .Where(hierarchy => hierarchy.DescendantId == ancestor.BasicId)
            .ToArray();
        var descendantClosures = workingHierarchies
            .Where(hierarchy => hierarchy.AncestorId == descendant.BasicId)
            .ToArray();
        var existingPairs = workingHierarchies
            .Select(hierarchy => new HierarchyPair(hierarchy.AncestorId, hierarchy.DescendantId))
            .ToHashSet();

        foreach (var ancestorClosure in ancestorClosures)
        {
            foreach (var descendantClosure in descendantClosures)
            {
                var pair = new HierarchyPair(ancestorClosure.AncestorId, descendantClosure.DescendantId);
                if (existingPairs.Contains(pair))
                {
                    continue;
                }

                var isDirectEdge = pair.AncestorId == ancestor.BasicId && pair.DescendantId == descendant.BasicId;
                var hierarchy = new SysRoleHierarchy
                {
                    AncestorId = pair.AncestorId,
                    DescendantId = pair.DescendantId,
                    Depth = ancestorClosure.Depth + 1 + descendantClosure.Depth,
                    Path = BuildCombinedPath(ancestorClosure, descendantClosure),
                    Remark = isDirectEdge ? NormalizeNullable(input.Remark) : null
                };

                addList.Add(hierarchy);
                workingHierarchies.Add(hierarchy);
                existingPairs.Add(pair);
            }
        }

        if (addList.Count == 0)
        {
            throw new InvalidOperationException("未生成新的角色继承闭包记录。");
        }

        _ = await _roleHierarchyRepository.AddRangeAsync(addList, cancellationToken);

        var directHierarchy = await _roleHierarchyRepository.GetFirstAsync(
            hierarchy => hierarchy.AncestorId == ancestor.BasicId && hierarchy.DescendantId == descendant.BasicId && hierarchy.Depth == 1,
            cancellationToken)
            ?? throw new InvalidOperationException("角色直接继承关系创建失败。");

        return RoleHierarchyApplicationMapper.ToDetailDto(directHierarchy, ancestor, descendant);
    }

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    /// <param name="id">角色继承主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Delete)]
    public async Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directHierarchy = await GetDirectHierarchyOrThrowAsync(id, cancellationToken);
        var descendant = await GetRoleForHierarchyOrThrowAsync(directHierarchy.DescendantId, cancellationToken);
        EnsureDescendantCanBeMaintainedForHierarchy(descendant);

        var existingHierarchies = (await _roleHierarchyRepository.GetAllAsync(cancellationToken)).ToList();
        var remainingDirectEdges = existingHierarchies
            .Where(hierarchy => hierarchy.Depth == 1 && hierarchy.BasicId != directHierarchy.BasicId)
            .Where(hierarchy => hierarchy.AncestorId != directHierarchy.AncestorId || hierarchy.DescendantId != directHierarchy.DescendantId)
            .ToArray();
        var remainingPairs = BuildReachablePairs(remainingDirectEdges);
        var directPair = new HierarchyPair(directHierarchy.AncestorId, directHierarchy.DescendantId);
        if (remainingPairs.Contains(directPair))
        {
            throw new InvalidOperationException("该直接继承关系存在替代路径，需先清理替代路径后再删除。");
        }

        var impactedAncestorIds = existingHierarchies
            .Where(hierarchy => hierarchy.DescendantId == directHierarchy.AncestorId)
            .Select(hierarchy => hierarchy.AncestorId)
            .Append(directHierarchy.AncestorId)
            .Distinct()
            .ToHashSet();
        var impactedDescendantIds = existingHierarchies
            .Where(hierarchy => hierarchy.AncestorId == directHierarchy.DescendantId)
            .Select(hierarchy => hierarchy.DescendantId)
            .Append(directHierarchy.DescendantId)
            .Distinct()
            .ToHashSet();
        var deletePairs = existingHierarchies
            .Where(hierarchy => hierarchy.Depth > 0)
            .Select(hierarchy => new HierarchyPair(hierarchy.AncestorId, hierarchy.DescendantId))
            .Where(pair => impactedAncestorIds.Contains(pair.AncestorId))
            .Where(pair => impactedDescendantIds.Contains(pair.DescendantId))
            .Where(pair => !remainingPairs.Contains(pair))
            .Distinct()
            .ToArray();

        foreach (var pair in deletePairs)
        {
            var ancestorId = pair.AncestorId;
            var descendantId = pair.DescendantId;
            _ = await _roleHierarchyRepository.DeleteAsync(
                hierarchy => hierarchy.AncestorId == ancestorId && hierarchy.DescendantId == descendantId,
                cancellationToken);
        }
    }

    /// <summary>
    /// 获取角色（层级场景），不存在时抛出异常
    /// </summary>
    private async Task<SysRole> GetRoleForHierarchyOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        return await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");
    }

    /// <summary>
    /// 获取直接继承关系，不满足规则时抛出异常
    /// </summary>
    private async Task<SysRoleHierarchy> GetDirectHierarchyOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色继承主键必须大于 0。");
        }

        var hierarchy = await _roleHierarchyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色继承关系不存在。");

        if (hierarchy.Depth != 1)
        {
            throw new InvalidOperationException("只能删除直接继承关系。");
        }

        return hierarchy;
    }

    /// <summary>
    /// 确保角色存在自身闭包记录
    /// </summary>
    private static void EnsureSelfHierarchy(SysRole role, List<SysRoleHierarchy> workingHierarchies, List<SysRoleHierarchy> addList)
    {
        if (workingHierarchies.Any(hierarchy => hierarchy.AncestorId == role.BasicId && hierarchy.DescendantId == role.BasicId))
        {
            return;
        }

        var hierarchy = new SysRoleHierarchy
        {
            AncestorId = role.BasicId,
            DescendantId = role.BasicId,
            Depth = 0,
            Path = role.BasicId.ToString()
        };

        workingHierarchies.Add(hierarchy);
        if (!role.IsGlobal && role.RoleType != RoleType.System)
        {
            addList.Add(hierarchy);
        }
    }

    /// <summary>
    /// 校验角色层级创建参数
    /// </summary>
    private static void ValidateRoleHierarchyCreateInput(RoleHierarchyCreateDto input)
    {
        if (input.AncestorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "祖先角色主键必须大于 0。");
        }

        if (input.DescendantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "后代角色主键必须大于 0。");
        }

        if (input.AncestorId == input.DescendantId)
        {
            throw new InvalidOperationException("角色不能继承自己。");
        }
    }

    /// <summary>
    /// 校验后代角色是否允许通过本服务维护继承关系
    /// </summary>
    private static void EnsureDescendantCanBeMaintainedForHierarchy(SysRole descendant)
    {
        if (descendant.IsGlobal || descendant.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护继承关系。");
        }
    }

    /// <summary>
    /// 生成合并后的继承路径
    /// </summary>
    private static string BuildCombinedPath(SysRoleHierarchy ancestorClosure, SysRoleHierarchy descendantClosure)
    {
        var pathIds = new List<long>(BuildPathIds(ancestorClosure));
        pathIds.AddRange(BuildPathIds(descendantClosure));
        return string.Join("/", pathIds);
    }

    /// <summary>
    /// 解析闭包路径
    /// </summary>
    private static IReadOnlyList<long> BuildPathIds(SysRoleHierarchy hierarchy)
    {
        if (!string.IsNullOrWhiteSpace(hierarchy.Path))
        {
            var ids = hierarchy.Path
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => long.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToArray();

            if (ids.Length > 0 && ids[0] == hierarchy.AncestorId && ids[^1] == hierarchy.DescendantId)
            {
                return ids;
            }
        }

        return hierarchy.AncestorId == hierarchy.DescendantId
            ? [hierarchy.AncestorId]
            : [hierarchy.AncestorId, hierarchy.DescendantId];
    }

    /// <summary>
    /// 根据直接继承边计算可达闭包键
    /// </summary>
    private static HashSet<HierarchyPair> BuildReachablePairs(IEnumerable<SysRoleHierarchy> directEdges)
    {
        var edges = directEdges.ToArray();
        var adjacency = edges
            .GroupBy(edge => edge.AncestorId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.DescendantId).Distinct().ToArray());
        var nodes = edges
            .SelectMany(edge => new[] { edge.AncestorId, edge.DescendantId })
            .Distinct()
            .ToArray();
        var pairs = new HashSet<HierarchyPair>();

        foreach (var startNode in nodes)
        {
            pairs.Add(new HierarchyPair(startNode, startNode));

            var visited = new HashSet<long> { startNode };
            var queue = new Queue<long>();
            if (adjacency.TryGetValue(startNode, out var children))
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                {
                    continue;
                }

                pairs.Add(new HierarchyPair(startNode, current));

                if (!adjacency.TryGetValue(current, out var nextChildren))
                {
                    continue;
                }

                foreach (var child in nextChildren)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return pairs;
    }

    #endregion RoleHierarchy

    #region Shared Helpers

    /// <summary>
    /// 校验有效期
    /// </summary>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("失效时间必须晚于生效时间。");
        }
    }

    #endregion Shared Helpers

    /// <summary>
    /// 校验枚举值
    /// </summary>
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
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private readonly record struct HierarchyPair(long AncestorId, long DescendantId);
}
