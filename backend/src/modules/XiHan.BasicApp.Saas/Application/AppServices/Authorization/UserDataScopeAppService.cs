#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeAppService
// Guid:3e1ca035-c2a4-4fd8-b97a-d5428d4ce9d8
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
/// 用户数据范围命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户数据范围")]
public sealed class UserDataScopeAppService(
    IUserDataScopeRepository userDataScopeRepository,
    IDepartmentRepository departmentRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserDataScopeAppService
{
    /// <summary>
    /// 用户数据范围仓储
    /// </summary>
    private readonly IUserDataScopeRepository _userDataScopeRepository = userDataScopeRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 授予用户数据范围
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Grant)]
    public async Task<UserDataScopeDetailDto> CreateUserDataScopeAsync(UserDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, cancellationToken);
        var department = await GetDepartmentForScopeOrThrowAsync(input.DataScope, input.DepartmentId, cancellationToken);
        var departmentId = ResolveDepartmentId(input.DataScope, input.DepartmentId);

        await EnsureCanPersistScopeAsync(input.UserId, input.DataScope, departmentId, null, cancellationToken);

        var dataScope = new SysUserDataScope
        {
            UserId = input.UserId,
            DataScope = input.DataScope,
            DepartmentId = departmentId,
            IncludeChildren = input.DataScope == DataPermissionScope.Custom && input.IncludeChildren,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDataScope = await _userDataScopeRepository.AddAsync(dataScope, cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Update)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeAsync(UserDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var dataScope = await GetUserDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(dataScope.UserId, now, cancellationToken);
        var department = await GetDepartmentForScopeOrThrowAsync(input.DataScope, input.DepartmentId, cancellationToken);
        var departmentId = ResolveDepartmentId(input.DataScope, input.DepartmentId);

        await EnsureCanPersistScopeAsync(dataScope.UserId, input.DataScope, departmentId, dataScope.BasicId, cancellationToken);

        dataScope.DataScope = input.DataScope;
        dataScope.DepartmentId = departmentId;
        dataScope.IncludeChildren = input.DataScope == DataPermissionScope.Custom && input.IncludeChildren;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Status)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeStatusAsync(UserDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var dataScope = await GetUserDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = input.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(dataScope.UserId, now, cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(dataScope.UserId, cancellationToken);
        var department = dataScope.DataScope == DataPermissionScope.Custom && input.Status == ValidityStatus.Valid
            ? await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken)
            : await GetDepartmentOrDefaultAsync(dataScope.DepartmentId, cancellationToken);

        dataScope.Status = input.Status;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 撤销用户数据范围
    /// </summary>
    /// <param name="id">用户数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Revoke)]
    public async Task DeleteUserDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await GetUserDataScopeOrThrowAsync(id, cancellationToken);
        dataScope.Status = ValidityStatus.Invalid;

        _ = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
    }

    /// <summary>
    /// 获取用户数据范围绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">用户数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围实体</returns>
    private async Task<SysUserDataScope> GetUserDataScopeOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户数据范围绑定主键必须大于 0。");
        }

        return await _userDataScopeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户数据范围绑定不存在。");
    }

    /// <summary>
    /// 获取可维护数据范围的租户成员，不满足规则时抛出异常
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员实体</returns>
    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能维护数据范围。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能维护数据范围。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员数据范围必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能维护数据范围。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能维护数据范围。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 校验用户数据范围覆盖可持久化
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="dataScope">数据权限范围</param>
    /// <param name="departmentId">部门主键</param>
    /// <param name="excludeId">排除的绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureCanPersistScopeAsync(long userId, DataPermissionScope dataScope, long departmentId, long? excludeId, CancellationToken cancellationToken)
    {
        var hasOtherMode = dataScope == DataPermissionScope.Custom
            ? await _userDataScopeRepository.AnyAsync(
                scope => scope.UserId == userId && scope.DataScope != DataPermissionScope.Custom && (!excludeId.HasValue || scope.BasicId != excludeId.Value),
                cancellationToken)
            : await _userDataScopeRepository.AnyAsync(
                scope => scope.UserId == userId && (!excludeId.HasValue || scope.BasicId != excludeId.Value),
                cancellationToken);

        if (hasOtherMode)
        {
            throw new InvalidOperationException("用户数据范围覆盖模式冲突。");
        }

        if (await _userDataScopeRepository.AnyAsync(
            scope => scope.UserId == userId && scope.DepartmentId == departmentId && (!excludeId.HasValue || scope.BasicId != excludeId.Value),
            cancellationToken))
        {
            throw new InvalidOperationException("用户数据范围已绑定。");
        }
    }

    /// <summary>
    /// 获取数据范围对应部门，不满足规则时抛出异常
    /// </summary>
    /// <param name="dataScope">数据权限范围</param>
    /// <param name="departmentId">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门实体</returns>
    private async Task<SysDepartment?> GetDepartmentForScopeOrThrowAsync(DataPermissionScope dataScope, long? departmentId, CancellationToken cancellationToken)
    {
        if (dataScope == DataPermissionScope.Custom)
        {
            if (!departmentId.HasValue || departmentId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(departmentId), "自定义用户数据范围必须指定部门主键。");
            }

            return await GetEnabledDepartmentOrThrowAsync(departmentId.Value, cancellationToken);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            throw new InvalidOperationException("非自定义用户数据范围不能指定部门。");
        }

        return null;
    }

    /// <summary>
    /// 获取已启用部门，不满足规则时抛出异常
    /// </summary>
    /// <param name="departmentId">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门实体</returns>
    private async Task<SysDepartment> GetEnabledDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能绑定到用户数据范围。");
        }

        return department;
    }

    /// <summary>
    /// 按需获取部门
    /// </summary>
    /// <param name="departmentId">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门实体</returns>
    private async Task<SysDepartment?> GetDepartmentOrDefaultAsync(long departmentId, CancellationToken cancellationToken)
    {
        return departmentId > 0
            ? await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            : null;
    }

    /// <summary>
    /// 解析持久化部门主键
    /// </summary>
    /// <param name="dataScope">数据权限范围</param>
    /// <param name="departmentId">部门主键</param>
    /// <returns>持久化部门主键</returns>
    private static long ResolveDepartmentId(DataPermissionScope dataScope, long? departmentId)
    {
        return dataScope == DataPermissionScope.Custom ? departmentId!.Value : 0;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(UserDataScopeGrantDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ValidateEnum(input.DataScope, nameof(input.DataScope));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(UserDataScopeUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(input.DataScope, nameof(input.DataScope));
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="paramName">参数名</param>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
