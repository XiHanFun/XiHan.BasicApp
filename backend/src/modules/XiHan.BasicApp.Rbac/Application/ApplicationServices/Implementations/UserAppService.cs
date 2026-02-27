using Mapster;
using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.ValueObjects;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 用户应用服务
/// </summary>
public class UserAppService : ApplicationServiceBase, IUserAppService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;
    private readonly IUserManager _userManager;
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IUserDepartmentRepository _userDepartmentRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuthorizationDomainService _authorizationDomainService;
    private readonly ILoginLogRepository _loginLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="userManager"></param>
    /// <param name="userSecurityRepository"></param>
    /// <param name="userRoleRepository"></param>
    /// <param name="userPermissionRepository"></param>
    /// <param name="userDepartmentRepository"></param>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="authorizationDomainService"></param>
    /// <param name="loginLogRepository"></param>
    /// <param name="passwordHasher"></param>
    /// <param name="unitOfWorkManager"></param>
    public UserAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IUserSecurityRepository userSecurityRepository,
        IUserRoleRepository userRoleRepository,
        IUserPermissionRepository userPermissionRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IAuthorizationDomainService authorizationDomainService,
        ILoginLogRepository loginLogRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _userSecurityRepository = userSecurityRepository;
        _userRoleRepository = userRoleRepository;
        _userPermissionRepository = userPermissionRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _authorizationDomainService = authorizationDomainService;
        _loginLogRepository = loginLogRepository;
        _passwordHasher = passwordHasher;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 根据用户ID获取用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<UserDto?> GetByIdAsync(long userId)
    {
        var entity = await _userRepository.GetByIdAsync(userId);
        return entity?.Adapt<UserDto>();
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
    /// 创建用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<UserDto> CreateAsync(UserCreateDto input)
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
        return created.Adapt<UserDto>();
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<UserDto> UpdateAsync(UserUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.BasicId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(input.BasicId));
        }

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
        return updated.Adapt<UserDto>();
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAsync(long userId)
    {
        if (userId <= 0)
        {
            return false;
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

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<UserLoginResultDto> LoginAsync(UserLoginCommand command)
    {
        command.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByUserNameAsync(command.UserName.Trim(), command.TenantId);
        if (user is null)
        {
            await WriteLoginLogAsync(0, command, LoginResult.InvalidCredentials, "用户名或密码错误");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                LoginResult = LoginResult.InvalidCredentials,
                Message = "用户名或密码错误"
            };
        }

        if (user.Status != YesOrNo.Yes)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountDisabled, "账号已禁用");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                UserId = user.BasicId,
                UserName = user.UserName,
                LoginResult = LoginResult.AccountDisabled,
                Message = "账号已禁用"
            };
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.IsLocked && security.LockoutEndTime.HasValue && security.LockoutEndTime > DateTimeOffset.UtcNow)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountLocked, "账号已锁定");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                UserId = user.BasicId,
                UserName = user.UserName,
                LoginResult = LoginResult.AccountLocked,
                Message = $"账号已锁定，请 {security.LockoutEndTime:HH:mm} 后重试"
            };
        }

        var password = PasswordValueObject.FromHash(user.Password);
        if (!password.Verify(command.Password, _passwordHasher))
        {
            await HandlePasswordFailureAsync(security);
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.InvalidCredentials, "用户名或密码错误");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                UserId = user.BasicId,
                UserName = user.UserName,
                LoginResult = LoginResult.InvalidCredentials,
                Message = "用户名或密码错误"
            };
        }

        security.FailedLoginAttempts = 0;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;
        security.LastFailedLoginTime = null;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userSecurityRepository.UpdateAsync(security);

        user.LastLoginTime = DateTimeOffset.UtcNow;
        user.LastLoginIp = command.LoginIp;
        await _userRepository.UpdateAsync(user);

        var permissionCodes = await _authorizationDomainService.GetUserPermissionCodesAsync(user.BasicId, user.TenantId);
        await WriteLoginLogAsync(user.BasicId, command, LoginResult.Success, "登录成功");
        await uow.CompleteAsync();

        return new UserLoginResultDto
        {
            Success = true,
            UserId = user.BasicId,
            UserName = user.UserName,
            LoginResult = LoginResult.Success,
            Message = "登录成功",
            PermissionCodes = permissionCodes
        };
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task ChangePasswordAsync(ChangePasswordCommand command)
    {
        command.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var currentPassword = PasswordValueObject.FromHash(user.Password);
        if (!currentPassword.Verify(command.OldPassword, _passwordHasher))
        {
            throw new InvalidOperationException("原密码错误");
        }

        await _userManager.ChangePasswordAsync(user, command.NewPassword);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 分配角色
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

        var roleIds = command.RoleIds.Distinct().ToArray();
        if (roleIds.Length > 0)
        {
            var roles = await _roleRepository.GetByIdsAsync(roleIds);
            if (roles.Count != roleIds.Length)
            {
                throw new InvalidOperationException("存在无效角色 ID");
            }
        }

        await _userRoleRepository.RemoveByUserIdAsync(command.UserId, command.TenantId ?? user.TenantId);
        if (roleIds.Length > 0)
        {
            var mappings = roleIds.Select(roleId => new SysUserRole
            {
                TenantId = command.TenantId ?? user.TenantId,
                UserId = command.UserId,
                RoleId = roleId,
                Status = YesOrNo.Yes
            }).ToArray();

            await _userRoleRepository.AddRangeAsync(mappings);
        }

        user.MarkRolesChanged(roleIds);
        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 分配权限
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

        var permissionIds = command.PermissionIds.Distinct().ToArray();
        if (permissionIds.Length > 0)
        {
            var permissions = await _permissionRepository.GetByIdsAsync(permissionIds);
            if (permissions.Count != permissionIds.Length)
            {
                throw new InvalidOperationException("存在无效权限 ID");
            }
        }

        await _userPermissionRepository.RemoveByUserIdAsync(command.UserId, command.TenantId ?? user.TenantId);
        if (permissionIds.Length > 0)
        {
            var mappings = permissionIds.Select(permissionId => new SysUserPermission
            {
                TenantId = command.TenantId ?? user.TenantId,
                UserId = command.UserId,
                PermissionId = permissionId,
                PermissionAction = PermissionAction.Grant,
                Status = YesOrNo.Yes
            }).ToArray();

            await _userPermissionRepository.AddRangeAsync(mappings);
        }

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 分配部门
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

        await _userDepartmentRepository.RemoveByUserIdAsync(command.UserId, command.TenantId ?? user.TenantId);

        var departmentIds = command.DepartmentIds.Distinct().ToArray();
        if (departmentIds.Length > 0)
        {
            var mappings = departmentIds.Select(departmentId => new SysUserDepartment
            {
                TenantId = command.TenantId ?? user.TenantId,
                UserId = command.UserId,
                DepartmentId = departmentId,
                IsMain = command.MainDepartmentId.HasValue && command.MainDepartmentId.Value == departmentId,
                Status = YesOrNo.Yes
            }).ToArray();

            await _userDepartmentRepository.AddRangeAsync(mappings);
        }

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _authorizationDomainService.GetUserPermissionCodesAsync(query.UserId, query.TenantId);
    }

    /// <summary>
    /// 写入登录日志
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="command"></param>
    /// <param name="loginResult"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task WriteLoginLogAsync(long userId, UserLoginCommand command, LoginResult loginResult, string message)
    {
        var log = new SysLoginLog
        {
            TenantId = command.TenantId,
            UserId = userId,
            UserName = command.UserName,
            LoginIp = command.LoginIp,
            Browser = command.Browser,
            Os = command.Os,
            LoginResult = loginResult,
            Message = message,
            LoginTime = DateTimeOffset.UtcNow
        };

        await _loginLogRepository.AddAsync(log);
    }

    /// <summary>
    /// 确保安全配置文件
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private async Task<SysUserSecurity> EnsureSecurityProfileAsync(SysUser user)
    {
        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId, user.TenantId);
        if (security is not null)
        {
            return security;
        }

        security = new SysUserSecurity
        {
            TenantId = user.TenantId,
            UserId = user.BasicId,
            FailedLoginAttempts = 0,
            IsLocked = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        return await _userSecurityRepository.AddAsync(security);
    }

    /// <summary>
    /// 处理密码失败
    /// </summary>
    /// <param name="security"></param>
    /// <returns></returns>
    private async Task HandlePasswordFailureAsync(SysUserSecurity security)
    {
        security.FailedLoginAttempts += 1;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;

        if (security.FailedLoginAttempts >= MaxFailedAttempts)
        {
            security.IsLocked = true;
            security.LockoutTime = DateTimeOffset.UtcNow;
            security.LockoutEndTime = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
        }

        await _userSecurityRepository.UpdateAsync(security);
    }
}
