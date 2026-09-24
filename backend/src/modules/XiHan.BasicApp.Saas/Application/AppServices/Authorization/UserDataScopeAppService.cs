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
    /// 批量变更用户数据范围（一次性提交授予与撤销，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Revoke)]
    public async Task BatchUpdateUserDataScopesAsync(UserDataScopeBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);
        _ = await _userDomainService.BatchUpdateUserDataScopesAsync(
            new UserDataScopeBatchUpdateCommand(
                input.UserId,
                [.. input.Grants.Select(grant => new UserDataScopeBatchGrantItem(grant.DepartmentId, grant.IncludeChildren))],
                input.RevokeUserDataScopeIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(input.UserId, cancellationToken);
    }

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Update)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeAsync(UserDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDataScopeAsync(UserDataScopeApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Status)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeStatusAsync(UserDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDataScopeStatusAsync(UserDataScopeApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    #endregion
}
