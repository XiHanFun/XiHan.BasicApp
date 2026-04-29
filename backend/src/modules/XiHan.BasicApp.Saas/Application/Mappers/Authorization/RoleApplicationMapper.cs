#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleApplicationMapper
// Guid:d003d0e0-bd62-4e3a-90f8-d2131db62bf4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 角色应用层映射器
/// </summary>
public static class RoleApplicationMapper
{
    /// <summary>
    /// 映射角色列表项
    /// </summary>
    /// <param name="role">角色定义</param>
    /// <returns>角色列表项 DTO</returns>
    public static RoleListItemDto ToListItemDto(SysRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleListItemDto
        {
            BasicId = role.BasicId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            RoleDescription = role.RoleDescription,
            RoleType = role.RoleType,
            IsGlobal = role.IsGlobal,
            DataScope = role.DataScope,
            MaxMembers = role.MaxMembers,
            Status = role.Status,
            Sort = role.Sort,
            CreatedTime = role.CreatedTime,
            ModifiedTime = role.ModifiedTime
        };
    }

    /// <summary>
    /// 映射角色详情
    /// </summary>
    /// <param name="role">角色定义</param>
    /// <returns>角色详情 DTO</returns>
    public static RoleDetailDto ToDetailDto(SysRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleDetailDto
        {
            BasicId = role.BasicId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            RoleDescription = role.RoleDescription,
            RoleType = role.RoleType,
            IsGlobal = role.IsGlobal,
            DataScope = role.DataScope,
            MaxMembers = role.MaxMembers,
            Status = role.Status,
            Sort = role.Sort,
            Remark = role.Remark,
            CreatedTime = role.CreatedTime,
            CreatedId = role.CreatedId,
            CreatedBy = role.CreatedBy,
            ModifiedTime = role.ModifiedTime,
            ModifiedId = role.ModifiedId,
            ModifiedBy = role.ModifiedBy
        };
    }

    /// <summary>
    /// 映射角色选择项
    /// </summary>
    /// <param name="role">角色定义</param>
    /// <returns>角色选择项 DTO</returns>
    public static RoleSelectItemDto ToSelectItemDto(SysRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleSelectItemDto
        {
            BasicId = role.BasicId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            RoleType = role.RoleType,
            IsGlobal = role.IsGlobal
        };
    }
}
