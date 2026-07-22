// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户应用层映射器
/// </summary>
public static class UserApplicationMapper
{
    /// <summary>
    /// 映射用户创建命令
    /// </summary>
    public static UserCreateCommand ToCreateCommand(UserCreateDto input, long? operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserCreateCommand(
            input.UserName,
            input.InitialPassword,
            input.RealName,
            input.NickName,
            input.Avatar,
            input.Email,
            input.Phone,
            input.Gender,
            input.Birthday,
            input.Status,
            input.TimeZone,
            input.Language,
            input.Country,
            input.MemberType,
            input.EffectiveTime,
            input.ExpirationTime,
            input.DisplayName,
            input.InviteRemark,
            input.Remark,
            operatorUserId);
    }

    /// <summary>
    /// 映射用户列表项（含批量预取的角色/部门/安全聚合数据）
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="roleNames">角色名称集合（已批量预取）</param>
    /// <param name="departmentName">主部门名称（已批量预取，可空）</param>
    /// <param name="isLocked">是否锁定（来自安全扩展）</param>
    /// <param name="twoFactorEnabled">是否启用双因素认证（来自安全扩展）</param>
    /// <returns>用户列表项 DTO</returns>
    public static UserListItemDto ToListItemDto(
        SysUser user,
        IReadOnlyList<string> roleNames,
        string? departmentName,
        bool isLocked,
        bool twoFactorEnabled)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roleNames);

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
            RoleNames = roleNames,
            DepartmentName = departmentName,
            IsLocked = isLocked,
            TwoFactorEnabled = twoFactorEnabled,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
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

    /// <summary>
    /// 映射用户状态变更命令
    /// </summary>
    public static UserStatusChangeCommand ToStatusCommand(UserStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new UserStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射用户更新命令
    /// </summary>
    public static UserUpdateCommand ToUpdateCommand(UserUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new UserUpdateCommand(
            input.BasicId,
            input.RealName,
            input.NickName,
            input.Avatar,
            input.Email,
            input.Phone,
            input.Gender,
            input.Birthday,
            input.TimeZone,
            input.Language,
            input.Country,
            input.Remark);
    }
}
