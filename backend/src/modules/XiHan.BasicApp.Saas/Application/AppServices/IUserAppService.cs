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

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户应用服务
/// </summary>
public interface IUserAppService
    : ICrudApplicationService<SysUser, UserDto, long, UserCreateDto, UserUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<UserDto?> GetByUserNameAsync(string userName, long? tenantId = null);

    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    Task<IReadOnlyList<UserRoleRelationDto>> GetUserRolesAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 获取用户权限关系
    /// </summary>
    Task<IReadOnlyList<UserPermissionRelationDto>> GetUserPermissionsAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    Task<IReadOnlyList<UserDepartmentRelationDto>> GetUserDepartmentsAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 分配用户角色
    /// </summary>
    Task AssignRolesAsync(AssignUserRolesCommand command);

    /// <summary>
    /// 分配用户权限
    /// </summary>
    Task AssignPermissionsAsync(AssignUserPermissionsCommand command);

    /// <summary>
    /// 分配用户部门
    /// </summary>
    Task AssignDepartmentsAsync(AssignUserDepartmentsCommand command);

    /// <summary>
    /// 修改用户状态
    /// </summary>
    Task ChangeStatusAsync(ChangeUserStatusCommand command);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task ResetPasswordAsync(ResetUserPasswordCommand command);
}
