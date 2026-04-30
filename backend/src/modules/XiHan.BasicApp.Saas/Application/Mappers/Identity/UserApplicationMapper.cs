#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserApplicationMapper
// Guid:5e760241-8945-4a7c-b325-a082231013b9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户应用层映射器
/// </summary>
public static class UserApplicationMapper
{
    /// <summary>
    /// 映射用户列表项
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>用户列表项 DTO</returns>
    public static UserListItemDto ToListItemDto(SysUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserListItemDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Gender = user.Gender,
            Status = user.Status,
            TimeZone = user.TimeZone,
            Language = user.Language,
            Country = user.Country,
            IsSystemAccount = user.IsSystemAccount,
            LastLoginTime = user.LastLoginTime,
            CreatedTime = user.CreatedTime,
            ModifiedTime = user.ModifiedTime
        };
    }

    /// <summary>
    /// 映射用户详情
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>用户详情 DTO</returns>
    public static UserDetailDto ToDetailDto(SysUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDetailDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Gender = user.Gender,
            Status = user.Status,
            LastLoginTime = user.LastLoginTime,
            TimeZone = user.TimeZone,
            Language = user.Language,
            Country = user.Country,
            IsSystemAccount = user.IsSystemAccount,
            Remark = user.Remark,
            CreatedTime = user.CreatedTime,
            CreatedId = user.CreatedId,
            CreatedBy = user.CreatedBy,
            ModifiedTime = user.ModifiedTime,
            ModifiedId = user.ModifiedId,
            ModifiedBy = user.ModifiedBy
        };
    }

    /// <summary>
    /// 映射用户选择项
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>用户选择项 DTO</returns>
    public static UserSelectItemDto ToSelectItemDto(SysUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserSelectItemDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Gender = user.Gender,
            IsSystemAccount = user.IsSystemAccount
        };
    }
}
