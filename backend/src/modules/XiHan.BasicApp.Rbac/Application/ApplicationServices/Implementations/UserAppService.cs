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
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 用户应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class UserAppService
    : CrudApplicationServiceBase<SysUser, UserDto, long, UserCreateDto, UserUpdateDto, BasicAppPRDto>,
        IUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserManager _userManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="userManager"></param>
    /// <param name="unitOfWorkManager"></param>
    public UserAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IUnitOfWorkManager unitOfWorkManager)
        : base(userRepository)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _unitOfWorkManager = unitOfWorkManager;
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
        return created.Adapt<UserDto>();
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<UserDto> UpdateAsync(long userId, UserUpdateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var user = await _userRepository.GetByIdAsync(userId)
                   ?? throw new KeyNotFoundException($"未找到用户: {userId}");

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
    public override async Task<bool> DeleteAsync(long userId)
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

}
