// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
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

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDepartmentAppService(
        IUserDomainService userDomainService,
        IPositionRepository positionRepository,
        IUserDepartmentRepository userDepartmentRepository,
        ILocalEventBus localEventBus)
    {
        _userDomainService = userDomainService;
        _positionRepository = positionRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _localEventBus = localEventBus;
    }

    #region 用户部门

    /// <summary>
    /// 批量变更用户部门归属（一次性提交分配与撤销，单事务）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Revoke)]
    public async Task BatchUpdateUserDepartmentsAsync(UserDepartmentBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var positionId in input.Assigns.Select(assign => assign.PositionId).Distinct())
        {
            await EnsurePositionExistsAsync(positionId, cancellationToken);
        }

        var result = await _userDomainService.BatchUpdateUserDepartmentsAsync(
            new UserDepartmentBatchUpdateCommand(
                input.UserId,
                [.. input.Assigns.Select(assign => new UserDepartmentBatchAssignItem(
                    assign.DepartmentId,
                    assign.IsMain,
                    assign.Remark,
                    assign.PositionId,
                    assign.JobNumber,
                    assign.JobLevel,
                    assign.JoinTime))],
                input.RevokeUserDepartmentIds),
            cancellationToken);

        // 部门归属变更事件：订阅方同步部门群成员（入部门自动进群，移出部门即踢出部门群）
        foreach (var departmentId in result.RevokedDepartmentIds)
        {
            await _localEventBus.PublishAsync(new UserDepartmentChangedDomainEvent(input.UserId, departmentId, isAssigned: false));
        }

        foreach (var departmentId in result.AssignedDepartmentIds)
        {
            await _localEventBus.PublishAsync(new UserDepartmentChangedDomainEvent(input.UserId, departmentId, isAssigned: true));
        }
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
