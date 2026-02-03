#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserApplicationService
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos.Users;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Services;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Application.Services;

/// <summary>
/// 用户应用服务
/// </summary>
public class UserApplicationService : CrudApplicationServiceBase<SysUser, SysUserDto, long, SysUserCreateDto, SysUserUpdateDto>
{
    private readonly ISysUserRepository _userRepository;
    private readonly IUserAuthenticationService _authenticationService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserApplicationService(
        ISysUserRepository userRepository,
        IUserAuthenticationService authenticationService,
        IRoleManagementService roleManagementService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(userRepository)
    {
        _userRepository = userRepository;
        _authenticationService = authenticationService;
        _roleManagementService = roleManagementService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public override async Task<SysUserDto> CreateAsync(SysUserCreateDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        // 检查用户名是否已存在
        var exists = await _userRepository.IsUserNameExistsAsync(input.UserName, null, input.TenantId);
        if (exists)
        {
            throw new InvalidOperationException($"用户名 {input.UserName} 已存在");
        }

        // 创建用户实体
        var user = input.Adapt<SysUser>();

        // 哈希密码
        user.Password = _authenticationService.HashPassword(input.Password);
        user.Status = YesOrNo.Yes;
        user.CreatedTime = DateTimeOffset.Now;

        // 保存用户
        user = await _userRepository.SaveAsync(user);

        //await uow.CompleteAsync();

        return user.Adapt<SysUserDto>();
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public override async Task<SysUserDto> UpdateAsync(long id, SysUserUpdateDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {id} 不存在");
        }

        // 映射更新数据
        input.Adapt(user);
        user.ModifiedTime = DateTimeOffset.Now;

        // 保存用户
        user = await _userRepository.SaveAsync(user);

        //await uow.CompleteAsync();

        return user.Adapt<SysUserDto>();
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<SysUserDto?> LoginAsync(UserLoginDto input)
    {
        var user = await _authenticationService.AuthenticateAsync(input.UserName, input.Password, input.TenantId);
        if (user == null)
        {
            return null;
        }

        // 更新最后登录信息
        await _userRepository.UpdateLastLoginInfoAsync(user.BasicId, string.Empty, DateTimeOffset.Now);

        return user.Adapt<SysUserDto>();
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<bool> ChangePasswordAsync(long userId, ChangePasswordDto input)
    {
        if (input.NewPassword != input.ConfirmPassword)
        {
            throw new InvalidOperationException("新密码与确认密码不一致");
        }

        //using var uow = _unitOfWorkManager.Begin();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        // 验证旧密码
        if (!_authenticationService.VerifyPassword(user, input.OldPassword))
        {
            throw new InvalidOperationException("旧密码不正确");
        }

        // 设置新密码
        user.Password = _authenticationService.HashPassword(input.NewPassword);
        user.ModifiedTime = DateTimeOffset.Now;

        await _userRepository.SaveAsync(user);
        //await uow.CompleteAsync();

        return true;
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    public async Task<bool> ResetPasswordAsync(long userId, string newPassword)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        user.Password = _authenticationService.HashPassword(newPassword);
        user.ModifiedTime = DateTimeOffset.Now;

        await _userRepository.SaveAsync(user);
        //await uow.CompleteAsync();

        return true;
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    public async Task<bool> EnableAsync(long userId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _userRepository.EnableUserAsync(userId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    public async Task<bool> DisableAsync(long userId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _userRepository.DisableUserAsync(userId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    public async Task<bool> AssignRolesAsync(AssignRolesToUserDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _roleManagementService.AssignRolesToUserAsync(input.UserId, input.RoleIds);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 获取租户下的用户列表
    /// </summary>
    public async Task<BasePageResultDto<SysUserDto>> GetUsersByTenantAsync(long tenantId, BasePageRequestDto input)
    {
        // 构建租户过滤条件
        var users = await _userRepository.GetUsersByTenantAsync(tenantId);
        var dtos = users.Adapt<List<SysUserDto>>();

        return new BasePageResultDto<SysUserDto>(dtos, new PageResultMetadata
        {
            PageIndex = 1,
            PageSize = dtos.Count,
            TotalCount = dtos.Count
        });
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<SysUserDto?> GetByUserNameAsync(string userName, long? tenantId = null)
    {
        var user = await _userRepository.GetByUserNameAsync(userName, tenantId);
        return user?.Adapt<SysUserDto>();
    }
}
