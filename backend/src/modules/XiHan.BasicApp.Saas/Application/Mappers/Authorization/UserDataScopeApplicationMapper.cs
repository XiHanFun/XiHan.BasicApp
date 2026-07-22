// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户数据范围应用层映射器
/// </summary>
public static class UserDataScopeApplicationMapper
{
    /// <summary>
    /// 映射用户数据范围授权命令
    /// </summary>
    public static UserDataScopeGrantCommand ToGrantCommand(UserDataScopeGrantDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserDataScopeGrantCommand(
            input.UserId,
            input.DepartmentId,
            input.IncludeChildren,
            input.Remark);
    }

    /// <summary>
    /// 映射用户数据范围列表项
    /// </summary>
    /// <param name="scope">用户数据范围覆盖</param>
    /// <param name="department">部门</param>
    /// <param name="tenantMember">租户成员</param>
    /// <returns>用户数据范围列表项 DTO</returns>
    public static UserDataScopeListItemDto ToListItemDto(SysUserDataScope scope, SysDepartment? department, SysTenantUser? tenantMember)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new UserDataScopeListItemDto
        {
            BasicId = scope.BasicId,
            UserId = scope.UserId,
            TenantMemberId = tenantMember?.BasicId,
            TenantMemberDisplayName = tenantMember?.DisplayName,
            TenantMemberType = tenantMember?.MemberType,
            TenantMemberInviteStatus = tenantMember?.InviteStatus,
            TenantMemberStatus = tenantMember?.Status,
            DepartmentId = scope.DepartmentId,
            DepartmentCode = department?.DepartmentCode,
            DepartmentName = department?.DepartmentName,
            ParentId = department?.ParentId,
            DepartmentType = department?.DepartmentType,
            DepartmentStatus = department?.Status,
            IncludeChildren = scope.IncludeChildren,
            Status = scope.Status,
            Remark = scope.Remark,
            CreatedTime = scope.CreatedTime
        };
    }

    /// <summary>
    /// 映射用户数据范围详情
    /// </summary>
    /// <param name="scope">用户数据范围覆盖</param>
    /// <param name="department">部门</param>
    /// <param name="tenantMember">租户成员</param>
    /// <returns>用户数据范围详情 DTO</returns>
    public static UserDataScopeDetailDto ToDetailDto(SysUserDataScope scope, SysDepartment? department, SysTenantUser? tenantMember)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new UserDataScopeDetailDto
        {
            BasicId = scope.BasicId,
            UserId = scope.UserId,
            TenantMemberId = tenantMember?.BasicId,
            TenantMemberDisplayName = tenantMember?.DisplayName,
            TenantMemberType = tenantMember?.MemberType,
            TenantMemberInviteStatus = tenantMember?.InviteStatus,
            TenantMemberStatus = tenantMember?.Status,
            DepartmentId = scope.DepartmentId,
            DepartmentCode = department?.DepartmentCode,
            DepartmentName = department?.DepartmentName,
            ParentId = department?.ParentId,
            DepartmentType = department?.DepartmentType,
            DepartmentStatus = department?.Status,
            IncludeChildren = scope.IncludeChildren,
            Status = scope.Status,
            Remark = scope.Remark,
            CreatedTime = scope.CreatedTime,
            CreatedId = scope.CreatedId,
            CreatedBy = scope.CreatedBy
        };
    }

    /// <summary>
    /// 映射用户数据范围状态变更命令
    /// </summary>
    public static UserDataScopeStatusChangeCommand ToStatusCommand(UserDataScopeStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new UserDataScopeStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射用户数据范围更新命令
    /// </summary>
    public static UserDataScopeUpdateCommand ToUpdateCommand(UserDataScopeUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserDataScopeUpdateCommand(
            input.BasicId,
            input.IncludeChildren,
            input.Remark);
    }
}
