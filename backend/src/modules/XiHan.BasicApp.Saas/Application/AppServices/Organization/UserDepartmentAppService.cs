#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentAppService
// Guid:2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f
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
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户部门命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户部门")]
public sealed class UserDepartmentAppService
    : SaasApplicationService, IUserDepartmentAppService
{
    private readonly IUserDomainService _userDomainService;

    private readonly IPositionRepository _positionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDepartmentAppService(IUserDomainService userDomainService, IPositionRepository positionRepository)
    {
        _userDomainService = userDomainService;
        _positionRepository = positionRepository;
    }

    #region 用户部门

    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Grant)]
    public async Task<UserDepartmentDetailDto> CreateUserDepartmentAsync(UserDepartmentAssignDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsurePositionExistsAsync(input.PositionId, cancellationToken);
        var result = await _userDomainService.CreateUserDepartmentAsync(UserDepartmentApplicationMapper.ToAssignCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
    }

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Revoke)]
    public async Task DeleteUserDepartmentAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserDepartmentAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Update)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentAsync(UserDepartmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsurePositionExistsAsync(input.PositionId, cancellationToken);
        var result = await _userDomainService.UpdateUserDepartmentAsync(UserDepartmentApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
    }

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Status)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentStatusAsync(UserDepartmentStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDepartmentStatusAsync(UserDepartmentApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
    }

    /// <summary>
    /// 校验岗位存在（仅在指定岗位时校验；岗位仓储自带租户过滤，跨租户岗位视为不存在）
    /// </summary>
    private async Task EnsurePositionExistsAsync(long? positionId, CancellationToken cancellationToken)
    {
        if (positionId is not > 0)
        {
            return;
        }

        _ = await _positionRepository.GetByIdAsync(positionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("岗位不存在。");
    }

    #endregion
}
