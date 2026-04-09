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
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 用户应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
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
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="departmentRepository"></param>
    /// <param name="userManager"></param>
    /// <param name="queryService"></param>
    /// <param name="unitOfWorkManager"></param>
    /// <param name="localEventBus"></param>
    public UserAppService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IDepartmentRepository departmentRepository,
        IUserManager userManager,
        IUserQueryService queryService,
        IUnitOfWorkManager unitOfWorkManager,
        ILocalEventBus localEventBus)
        : base(userRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
        _userManager = userManager;
        _queryService = queryService;
        _unitOfWorkManager = unitOfWorkManager;
        _localEventBus = localEventBus;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
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
    public async Task<UserDto?> GetByUserNameAsync(string userName, long? tenantId = null)
    {
        var entity = await _userRepository.GetByUserNameAsync(userName, tenantId);
        return entity?.Adapt<UserDto>();
    }

    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<UserRoleRelationDto>> GetUserRolesAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var relations = await _userRepository.GetUserRolesAsync(userId, tenantId);
        return relations.Select(relation => new UserRoleRelationDto
        {
            BasicId = relation.BasicId,
            TenantId = relation.TenantId,
            UserId = relation.UserId,
            RoleId = relation.RoleId,
            Status = relation.Status
        }).ToArray();
    }

    /// <summary>
    /// 获取用户权限关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<UserPermissionRelationDto>> GetUserPermissionsAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var relations = await _userRepository.GetUserPermissionsAsync(userId, tenantId);
        return relations.Select(relation => new UserPermissionRelationDto
        {
            BasicId = relation.BasicId,
            TenantId = relation.TenantId,
            UserId = relation.UserId,
            PermissionId = relation.PermissionId,
            PermissionAction = relation.PermissionAction,
            Status = relation.Status
        }).ToArray();
    }

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<UserDepartmentRelationDto>> GetUserDepartmentsAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var relations = await _userRepository.GetUserDepartmentsAsync(userId, tenantId);
        return relations.Select(relation => new UserDepartmentRelationDto
        {
            BasicId = relation.BasicId,
            TenantId = relation.TenantId,
            UserId = relation.UserId,
            DepartmentId = relation.DepartmentId,
            IsMain = relation.IsMain,
            Status = relation.Status
        }).ToArray();
    }

    /// <summary>
    /// 分配用户角色
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
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
        if (roleIds.Length > 0)
        {
            var roles = await _roleRepository.GetByIdsAsync(roleIds);
            if (roles.Count != roleIds.Length)
            {
                throw new BusinessException(message: "存在无效角色 ID");
            }
        }

        await _userRepository.ReplaceUserRolesAsync(
            command.UserId,
            roleIds,
            command.TenantId ?? user.TenantId);

        user.MarkRolesChanged(roleIds);
        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
        await PublishAuthorizationChangedEventAsync(command.TenantId ?? user.TenantId, AuthorizationChangeType.Permission);
    }

    /// <summary>
    /// 分配用户权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
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
        await PublishAuthorizationChangedEventAsync(command.TenantId ?? user.TenantId, AuthorizationChangeType.Permission);
    }

    /// <summary>
    /// 分配用户部门
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
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
        await PublishAuthorizationChangedEventAsync(command.TenantId ?? user.TenantId, AuthorizationChangeType.DataScope);
    }

    /// <summary>
    /// 修改用户状态
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
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
    public override async Task<UserDto> CreateAsync(UserCreateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var user = new SysUser
        {
            TenantId = input.TenantId,
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
        return created.Adapt<UserDto>()!;
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
            user.Disable();
        }

        var updated = await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
        return updated.Adapt<UserDto>()!;
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
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

        await _userRepository.DeleteAsync(user);
        await uow.CompleteAsync();
        return true;
    }

    private Task PublishAuthorizationChangedEventAsync(long? tenantId, AuthorizationChangeType changeType)
    {
        return _localEventBus.PublishAsync(new RbacAuthorizationChangedEvent(tenantId, changeType));
    }
}
