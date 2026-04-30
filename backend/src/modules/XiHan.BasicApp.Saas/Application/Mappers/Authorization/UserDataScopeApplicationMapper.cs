#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeApplicationMapper
// Guid:892cf501-5459-4769-af38-5127dbe5c92b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户数据范围应用层映射器
/// </summary>
public static class UserDataScopeApplicationMapper
{
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
            DataScope = scope.DataScope,
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
            DataScope = scope.DataScope,
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
}
