#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAppService
// Guid:aad51771-2989-4c05-ab6d-5f9a3e594a8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 用户应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("user:read")]
public class UserAppService
    : CrudApplicationServiceBase<SysUser, UserDto, long, UserCreateDto, UserUpdateDto, BasicAppPRDto>,
        IUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserManager _userManager;
    private readonly IUserQueryService _queryService;
    private readonly ISuperAdminGuard _superAdminGuard;
    private readonly IRbacChangeNotifier _rbacChangeNotifier;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="departmentRepository"></param>
    /// <param name="userManager"></param>
    /// <param name="queryService"></param>
    /// <param name="superAdminGuard"></param>
    /// <param name="rbacChangeNotifier"></param>
    /// <param name="unitOfWorkManager"></param>
    public UserAppService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IDepartmentRepository departmentRepository,
        IUserManager userManager,
        IUserQueryService queryService,
        ISuperAdminGuard superAdminGuard,
        IRbacChangeNotifier rbacChangeNotifier,
        IUnitOfWorkManager unitOfWorkManager)
        : base(userRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
        _userManager = userManager;
        _queryService = queryService;
        _superAdminGuard = superAdminGuard;
        _rbacChangeNotifier = rbacChangeNotifier;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    [PermissionAuthorize("user:read")]
    public override async Task<UserDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:read")]
    public async Task<UserDto?> GetByUserNameAsync(string userName, long? tenantId = null)
    {
        return await _queryService.GetByUserNameAsync(userName, tenantId);
    }

    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:read")]
    public async Task<IReadOnlyList<UserRoleRelationDto>> GetUserRolesAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        return await _queryService.GetUserRolesAsync(userId, tenantId);
    }

    /// <summary>
    /// 获取用户权限关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:read")]
    public async Task<IReadOnlyList<UserPermissionRelationDto>> GetUserPermissionsAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        return await _queryService.GetUserPermissionsAsync(userId, tenantId);
    }

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:read")]
    public async Task<IReadOnlyList<UserDepartmentRelationDto>> GetUserDepartmentsAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        return await _queryService.GetUserDepartmentsAsync(userId, tenantId);
    }

    /// <summary>
    /// 分配用户角色
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:update")]
    public async Task AssignRolesAsync(AssignUserRolesCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(command.UserId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var roleIds = command.RoleIds.Where(id => id > 0).Distinct().ToArray();
        var roles = Array.Empty<SysRole>();
        if (roleIds.Length > 0)
        {
            roles = [.. await _roleRepository.GetByIdsAsync(roleIds)];
            if (roles.Length != roleIds.Length)
            {
                throw new BusinessException(message: "存在无效角色 ID");
            }
        }
        await _superAdminGuard.EnsureRoleAssignmentAllowedAsync(user, roleIds, roles, command.TenantId ?? user.TenantId);

        await _userRepository.ReplaceUserRolesAsync(
            command.UserId,
            roleIds,
            command.TenantId ?? user.TenantId);

        user.MarkRolesChanged(roleIds);
        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
        await _rbacChangeNotifier.NotifyAsync(command.TenantId ?? user.TenantId, AuthorizationChangeType.Permission);
    }

    /// <summary>
    /// 分配用户权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:update")]
    public async Task AssignPermissionsAsync(AssignUserPermissionsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(command.UserId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var permissionIds = command.PermissionIds.Where(id => id > 0).Distinct().ToArray();
        if (permissionIds.Length > 0)
        {
            var permissions = await _permissionRepository.GetByIdsAsync(permissionIds);
            if (permissions.Count != permissionIds.Length)
            {
                throw new BusinessException(message: "存在无效权限 ID");
            }
        }

        await _userRepository.ReplaceUserPermissionsAsync(
            command.UserId,
            permissionIds,
            command.TenantId ?? user.TenantId);

        user.MarkPermissionsChanged(permissionIds);
        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
        await _rbacChangeNotifier.NotifyAsync(command.TenantId ?? user.TenantId, AuthorizationChangeType.Permission);
    }

    /// <summary>
    /// 分配用户部门
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:update")]
    public async Task AssignDepartmentsAsync(AssignUserDepartmentsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(command.UserId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var departmentIds = command.DepartmentIds.Where(id => id > 0).Distinct().ToArray();
        if (departmentIds.Length > 0)
        {
            var departments = await _departmentRepository.GetByIdsAsync(departmentIds);
            if (departments.Count != departmentIds.Length)
            {
                throw new BusinessException(message: "存在无效部门 ID");
            }
        }

        if (command.MainDepartmentId.HasValue
            && departmentIds.Length > 0
            && !departmentIds.Contains(command.MainDepartmentId.Value))
        {
            throw new BusinessException(message: "主部门必须在分配的部门范围内");
        }

        await _userRepository.ReplaceUserDepartmentsAsync(
            command.UserId,
            departmentIds,
            command.MainDepartmentId,
            command.TenantId ?? user.TenantId);

        user.MarkDepartmentsChanged(departmentIds, command.MainDepartmentId);
        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
        await _rbacChangeNotifier.NotifyAsync(command.TenantId ?? user.TenantId, AuthorizationChangeType.DataScope);
    }

    /// <summary>
    /// 修改用户状态
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:update")]
    public async Task ChangeStatusAsync(ChangeUserStatusCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(command.UserId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        if (command.Status != YesOrNo.Yes)
        {
            await _superAdminGuard.EnsureAccountMutableAsync(user, user.TenantId, "禁用");
        }

        if (command.Status == YesOrNo.Yes)
        {
            user.Enable();
        }
        else
        {
            user.Disable();
        }

        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:update")]
    public async Task ResetPasswordAsync(ResetUserPasswordCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(command.UserId));
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            throw new ArgumentException("新密码不能为空", nameof(command.NewPassword));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        await _userManager.ChangePasswordAsync(user, command.NewPassword);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:create")]
    public override async Task<UserDto> CreateAsync(UserCreateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var user = new SysUser
        {
            TenantId = input.TenantId ?? 0,
            UserName = input.UserName.Trim(),
            RealName = input.RealName,
            NickName = input.NickName,
            Email = input.Email,
            Phone = input.Phone,
            Gender = input.Gender,
            Status = YesOrNo.Yes
        };

        var created = await _userManager.CreateAsync(user, input.Password);
        await uow.CompleteAsync();
        return await MapUserToDtoAsync(created);
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:update")]
    public override async Task<UserDto> UpdateAsync(UserUpdateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var user = await _userRepository.GetByIdAsync(input.BasicId)
                   ?? throw new KeyNotFoundException($"未找到用户: {input.BasicId}");

        user.RealName = input.RealName;
        user.NickName = input.NickName;
        user.Email = input.Email;
        user.Phone = input.Phone;
        user.Gender = input.Gender;
        user.Avatar = input.Avatar;
        user.Remark = input.Remark;

        if (input.Status == YesOrNo.Yes)
        {
            user.Enable();
        }
        else
        {
            await _superAdminGuard.EnsureAccountMutableAsync(user, user.TenantId, "禁用");
            user.Disable();
        }

        var updated = await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
        return await MapUserToDtoAsync(updated);
    }

    /// <summary>
    /// 映射实体到 DTO，补齐用户角色与基础展示字段。
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected override async Task<UserDto> MapEntityToDtoAsync(SysUser entity)
    {
        return await MapUserToDtoAsync(entity);
    }

    /// <summary>
    /// 批量映射实体到 DTO，避免逐条查询角色关系。
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    protected override async Task<IList<UserDto>> MapEntitiesToDtosAsync(IEnumerable<SysUser> entities)
    {
        var users = entities as SysUser[] ?? [.. entities];
        if (users.Length == 0)
        {
            return [];
        }

        var roleIdsMap = await _userRepository.GetRoleIdsMapByUserIdsAsync(users.Select(user => user.BasicId).ToArray());
        return users
            .Select(user =>
            {
                roleIdsMap.TryGetValue(user.BasicId, out var roleIds);
                return MapUser(user, roleIds);
            })
            .ToArray();
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [PermissionAuthorize("user:delete")]
    public override async Task<bool> DeleteAsync(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return false;
        }
        await _superAdminGuard.EnsureAccountMutableAsync(user, user.TenantId, "删除");

        await _userRepository.DeleteAsync(user);
        await uow.CompleteAsync();
        return true;
    }

    private async Task<UserDto> MapUserToDtoAsync(SysUser user)
    {
        var relations = await _userRepository.GetUserRolesAsync(user.BasicId, user.TenantId);
        return MapUser(user, relations.Select(relation => relation.RoleId).Distinct().ToArray());
    }

    private static UserDto MapUser(SysUser user, IReadOnlyList<long>? roleIds)
    {
        var dto = user.Adapt<UserDto>()!;
        dto.RoleIds = roleIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
        return dto;
    }

}
