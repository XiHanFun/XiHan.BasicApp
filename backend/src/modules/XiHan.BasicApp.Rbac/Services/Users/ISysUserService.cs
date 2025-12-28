#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserService
// Guid:ea2b3c4d-5e6f-7890-abcd-ef12345678a3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Services.Users.Dtos;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Users;

/// <summary>
/// 系统用户服务接口
/// </summary>
public interface ISysUserService : ICrudApplicationService<UserDto, XiHanBasicAppIdType, CreateUserDto, UpdateUserDto>
{
    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns></returns>
    Task<UserDetailDto?> GetDetailAsync(XiHanBasicAppIdType id);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns></returns>
    Task<UserDto?> GetByUserNameAsync(string userName);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns></returns>
    Task<UserDto?> GetByEmailAsync(string email);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns></returns>
    Task<UserDto?> GetByPhoneAsync(string phone);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="input">修改密码DTO</param>
    /// <returns></returns>
    Task<bool> ChangePasswordAsync(ChangePasswordDto input);

    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="input">重置密码DTO</param>
    /// <returns></returns>
    Task<bool> ResetPasswordAsync(ResetPasswordDto input);

    /// <summary>
    /// 分配角色
    /// </summary>
    /// <param name="input">分配角色DTO</param>
    /// <returns></returns>
    Task<bool> AssignRolesAsync(AssignUserRolesDto input);

    /// <summary>
    /// 分配部门
    /// </summary>
    /// <param name="input">分配部门DTO</param>
    /// <returns></returns>
    Task<bool> AssignDepartmentsAsync(AssignUserDepartmentsDto input);

    /// <summary>
    /// 获取用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<string>> GetUserPermissionsAsync(XiHanBasicAppIdType userId);
}
