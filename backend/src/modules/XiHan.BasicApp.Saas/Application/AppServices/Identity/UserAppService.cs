#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAppService
// Guid:ce9953f4-f7d4-4ebe-96cc-e282947f11ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserAppService
    : SaasApplicationService, IUserAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly IUserDomainService _userDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserAppService(
        IUserDomainService userDomainService,
        ICurrentUser currentUser)
    {
        _userDomainService = userDomainService;
        _currentUser = currentUser;
    }

    #region 用户核心

    /// <summary>
    /// 创建用户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Create)]
    public async Task<UserDetailDto> CreateUserAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserAsync(
            UserApplicationMapper.ToCreateCommand(input, _currentUser.UserId),
            cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Delete)]
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Update)]
    public async Task<UserDetailDto> UpdateUserAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserAsync(UserApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Status)]
    public async Task<UserDetailDto> UpdateUserStatusAsync(UserStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserStatusAsync(UserApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    #endregion

}
