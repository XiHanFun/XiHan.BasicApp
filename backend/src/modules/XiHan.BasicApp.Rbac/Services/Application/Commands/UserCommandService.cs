#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserCommandService
// Guid:f1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c
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
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 用户命令服务（处理用户的写操作）
/// </summary>
public class UserCommandService : CrudApplicationServiceBase<SysUser, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IUserRepository _userRepository;
    private readonly UserDomainService _userDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserCommandService(
        IUserRepository userRepository,
        UserDomainService userDomainService)
        : base(userRepository)
    {
        _userRepository = userRepository;
        _userDomainService = userDomainService;
    }

    /// <summary>
    /// 创建用户（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        if (!await _userDomainService.IsUserNameUniqueAsync(input.UserName))
        {
            throw new InvalidOperationException($"用户名 {input.UserName} 已存在");
        }

        if (!string.IsNullOrEmpty(input.Email) && !await _userDomainService.IsEmailUniqueAsync(input.Email))
        {
            throw new InvalidOperationException($"邮箱 {input.Email} 已被使用");
        }

        if (!string.IsNullOrEmpty(input.Phone) && !await _userDomainService.IsPhoneUniqueAsync(input.Phone))
        {
            throw new InvalidOperationException($"手机号 {input.Phone} 已被使用");
        }

        // 2. 租户限制验证
        if (input.TenantId.HasValue)
        {
            await _userDomainService.ValidateTenantLimitAsync(input.TenantId.Value);
        }

        // 3. 映射并创建
        var user = input.Adapt<SysUser>();

        // 4. 密码加密（实际应该使用密码服务）
        // user.Password = HashPassword(input.Password);

        // 5. 保存
        user = await _userRepository.AddAsync(user);

        return await MapToEntityDtoAsync(user);
    }

    /// <summary>
    /// 更新用户（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取用户
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {id} 不存在");
        }

        // 2. 业务验证
        if (user.UserName != input.UserName && !await _userDomainService.IsUserNameUniqueAsync(input.UserName, id))
        {
            throw new InvalidOperationException($"用户名 {input.UserName} 已存在");
        }

        if (!string.IsNullOrEmpty(input.Email) && user.Email != input.Email &&
            !await _userDomainService.IsEmailUniqueAsync(input.Email, id))
        {
            throw new InvalidOperationException($"邮箱 {input.Email} 已被使用");
        }

        if (!string.IsNullOrEmpty(input.Phone) && user.Phone != input.Phone &&
            !await _userDomainService.IsPhoneUniqueAsync(input.Phone, id))
        {
            throw new InvalidOperationException($"手机号 {input.Phone} 已被使用");
        }

        // 3. 更新实体
        input.Adapt(user);

        // 4. 保存
        user = await _userRepository.UpdateAsync(user);

        return await MapToEntityDtoAsync(user);
    }

    /// <summary>
    /// 分配角色给用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AssignRolesToUserAsync(long userId, List<long> roleIds)
    {
        // 1. 领域服务验证
        await _userDomainService.AssignRolesToUserAsync(userId, roleIds);

        // 2. 实际的关系维护（需要通过仓储或关系表服务）
        // 这里只是示例，实际实现需要根据具体的关系表处理

        return true;
    }

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <returns>是否成功</returns>
    public async Task<bool> RemoveRolesFromUserAsync(long userId, List<long> roleIds)
    {
        // 1. 领域服务验证
        await _userDomainService.RemoveRolesFromUserAsync(userId, roleIds);

        // 2. 实际的关系维护

        return true;
    }

    /// <summary>
    /// 授予用户直接权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <returns>是否成功</returns>
    public async Task<bool> GrantPermissionsToUserAsync(long userId, List<long> permissionIds)
    {
        // 1. 领域服务验证
        await _userDomainService.GrantPermissionsToUserAsync(userId, permissionIds);

        // 2. 实际的关系维护

        return true;
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="newPassword">新密码</param>
    /// <returns>是否成功</returns>
    public async Task<bool> ResetPasswordAsync(long userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        // 密码加密（实际应该使用密码服务）
        // user.Password = HashPassword(newPassword);

        await _userRepository.UpdateAsync(user);
        return true;
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long userId, Enums.YesOrNo status)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        user.Status = status;
        await _userRepository.UpdateAsync(user);
        return true;
    }
}
