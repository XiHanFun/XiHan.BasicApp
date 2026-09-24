// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户数据范围命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户数据范围")]
public sealed class UserDataScopeAppService
    : SaasApplicationService, IUserDataScopeAppService
{
    private readonly IUserDomainService _userDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDataScopeAppService(
        IUserDomainService userDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        ISuperAdminProtector superAdminProtector)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
        _superAdminProtector = superAdminProtector;
    }

    #region 用户数据范围

    /// <summary>
    /// 设置成员在本租户的数据范围：覆盖档位与自定义部门一次提交（单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Update)]
    public async Task SetUserDataScopeAsync(UserDataScopeSetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);
        _ = await _userDomainService.SetUserDataScopeAsync(
            new UserDataScopeSetCommand(
                input.UserId,
                input.DataScope,
                [.. input.Departments.Select(item => new DataScopeDepartmentItem(item.DepartmentId, item.IncludeChildren))]),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(input.UserId, cancellationToken);
    }

    #endregion
}
