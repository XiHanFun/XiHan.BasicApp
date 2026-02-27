#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserAppService
// Guid:0959ada9-f97b-40bb-a865-146a8b3658c9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:46:27
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 用户应用服务
/// </summary>
public interface IUserAppService : IApplicationService
{
    /// <summary>
    /// 根据用户ID获取用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<UserDto?> GetByIdAsync(long userId);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<UserDto?> GetByUserNameAsync(string userName, long? tenantId = null);

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<UserDto> CreateAsync(UserCreateDto input);

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<UserDto> UpdateAsync(UserUpdateDto input);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(long userId);

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task<UserLoginResultDto> LoginAsync(UserLoginCommand command);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task ChangePasswordAsync(ChangePasswordCommand command);

    /// <summary>
    /// 分配角色
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task AssignRolesAsync(AssignUserRolesCommand command);

    /// <summary>
    /// 分配权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task AssignPermissionsAsync(AssignUserPermissionsCommand command);

    /// <summary>
    /// 分配部门
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task AssignDepartmentsAsync(AssignUserDepartmentsCommand command);

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query);
}
