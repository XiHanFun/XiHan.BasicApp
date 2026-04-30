#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentAppService
// Guid:e76e1874-a735-4929-86cf-0602785b6a59
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户部门归属命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户部门")]
public sealed class UserDepartmentAppService(
    IUserDepartmentRepository userDepartmentRepository,
    IDepartmentRepository departmentRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserDepartmentAppService
{
    /// <summary>
    /// 用户部门仓储
    /// </summary>
    private readonly IUserDepartmentRepository _userDepartmentRepository = userDepartmentRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    /// <param name="input">分配参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Grant)]
    public async Task<UserDepartmentDetailDto> CreateUserDepartmentAsync(UserDepartmentAssignDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateAssignInput(input);

        var now = DateTimeOffset.UtcNow;
        _ = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, cancellationToken);
        var department = await GetAssignableDepartmentOrThrowAsync(input.DepartmentId, cancellationToken);
        var shouldBeMain = input.IsMain || !await HasValidDepartmentAsync(input.UserId, cancellationToken);

        var userDepartment = await _userDepartmentRepository.GetFirstAsync(
            relation => relation.UserId == input.UserId && relation.DepartmentId == input.DepartmentId,
            cancellationToken);
        if (userDepartment is not null && userDepartment.Status == ValidityStatus.Valid)
        {
            throw new InvalidOperationException("用户部门归属已存在。");
        }

        if (shouldBeMain)
        {
            await ClearOtherMainDepartmentsAsync(input.UserId, userDepartment?.BasicId, cancellationToken);
        }

        if (userDepartment is null)
        {
            userDepartment = new SysUserDepartment
            {
                UserId = input.UserId,
                DepartmentId = input.DepartmentId,
                IsMain = shouldBeMain,
                Status = ValidityStatus.Valid,
                Remark = NormalizeNullable(input.Remark)
            };

            var savedUserDepartment = await _userDepartmentRepository.AddAsync(userDepartment, cancellationToken);
            return UserDepartmentApplicationMapper.ToDetailDto(savedUserDepartment, department);
        }

        userDepartment.IsMain = shouldBeMain;
        userDepartment.Status = ValidityStatus.Valid;
        userDepartment.Remark = NormalizeNullable(input.Remark);

        var restoredUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(restoredUserDepartment, department);
    }

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Update)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentAsync(UserDepartmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var userDepartment = await GetUserDepartmentOrThrowAsync(input.BasicId, cancellationToken);
        if (userDepartment.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效用户部门归属不能更新。");
        }

        _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, cancellationToken);
        var department = await GetAssignableDepartmentOrThrowAsync(userDepartment.DepartmentId, cancellationToken);
        if (input.IsMain)
        {
            await ClearOtherMainDepartmentsAsync(userDepartment.UserId, userDepartment.BasicId, cancellationToken);
        }

        userDepartment.IsMain = input.IsMain;
        userDepartment.Remark = NormalizeNullable(input.Remark);

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(savedUserDepartment, department);
    }

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Status)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentStatusAsync(UserDepartmentStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户部门归属主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var userDepartment = await GetUserDepartmentOrThrowAsync(input.BasicId, cancellationToken);
        var department = input.Status == ValidityStatus.Valid
            ? await GetAssignableDepartmentOrThrowAsync(userDepartment.DepartmentId, cancellationToken)
            : await _departmentRepository.GetByIdAsync(userDepartment.DepartmentId, cancellationToken);

        if (input.Status == ValidityStatus.Valid)
        {
            _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, cancellationToken);
            if (userDepartment.IsMain)
            {
                await ClearOtherMainDepartmentsAsync(userDepartment.UserId, userDepartment.BasicId, cancellationToken);
            }
        }
        else
        {
            userDepartment.IsMain = false;
        }

        userDepartment.Status = input.Status;
        userDepartment.Remark = NormalizeNullable(input.Remark);

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        if (input.Status != ValidityStatus.Valid)
        {
            await PromoteMainDepartmentIfNeededAsync(savedUserDepartment.UserId, savedUserDepartment.BasicId, cancellationToken);
        }

        return UserDepartmentApplicationMapper.ToDetailDto(savedUserDepartment, department);
    }

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    /// <param name="id">用户部门归属主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Revoke)]
    public async Task DeleteUserDepartmentAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userDepartment = await GetUserDepartmentOrThrowAsync(id, cancellationToken);
        userDepartment.IsMain = false;
        userDepartment.Status = ValidityStatus.Invalid;

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        await PromoteMainDepartmentIfNeededAsync(savedUserDepartment.UserId, savedUserDepartment.BasicId, cancellationToken);
    }

    /// <summary>
    /// 获取用户部门归属，不存在时抛出异常
    /// </summary>
    private async Task<SysUserDepartment> GetUserDepartmentOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户部门归属主键必须大于 0。");
        }

        return await _userDepartmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户部门归属不存在。");
    }

    /// <summary>
    /// 获取可分配租户成员，不满足规则时抛出异常
    /// </summary>
    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能分配部门。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能分配部门。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员部门归属必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能分配部门。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能分配部门。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 获取可分配部门，不满足规则时抛出异常
    /// </summary>
    private async Task<SysDepartment> GetAssignableDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能分配给用户。");
        }

        return department;
    }

    /// <summary>
    /// 判断用户是否已有有效部门归属
    /// </summary>
    private async Task<bool> HasValidDepartmentAsync(long userId, CancellationToken cancellationToken)
    {
        return await _userDepartmentRepository.AnyAsync(
            relation => relation.UserId == userId && relation.Status == ValidityStatus.Valid,
            cancellationToken);
    }

    /// <summary>
    /// 清理用户其它主部门标记
    /// </summary>
    private async Task ClearOtherMainDepartmentsAsync(long userId, long? excludeId, CancellationToken cancellationToken)
    {
        var mainDepartments = await _userDepartmentRepository.GetListAsync(
            relation => relation.UserId == userId && relation.IsMain && (!excludeId.HasValue || relation.BasicId != excludeId.Value),
            cancellationToken);
        if (mainDepartments.Count == 0)
        {
            return;
        }

        foreach (var mainDepartment in mainDepartments)
        {
            mainDepartment.IsMain = false;
        }

        await _userDepartmentRepository.UpdateRangeAsync(mainDepartments, cancellationToken);
    }

    /// <summary>
    /// 用户撤销主部门后自动接续一个有效部门
    /// </summary>
    private async Task PromoteMainDepartmentIfNeededAsync(long userId, long revokedId, CancellationToken cancellationToken)
    {
        if (await _userDepartmentRepository.AnyAsync(
            relation => relation.UserId == userId && relation.Status == ValidityStatus.Valid && relation.IsMain,
            cancellationToken))
        {
            return;
        }

        var candidates = await _userDepartmentRepository.GetListAsync(
            relation => relation.UserId == userId && relation.Status == ValidityStatus.Valid && relation.BasicId != revokedId,
            relation => relation.CreatedTime,
            cancellationToken);
        var nextMain = candidates.FirstOrDefault();
        if (nextMain is null)
        {
            return;
        }

        nextMain.IsMain = true;
        _ = await _userDepartmentRepository.UpdateAsync(nextMain, cancellationToken);
    }

    /// <summary>
    /// 校验分配参数
    /// </summary>
    private static void ValidateAssignInput(UserDepartmentAssignDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "部门主键必须大于 0。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(UserDepartmentUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户部门归属主键必须大于 0。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
