// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 用户的响应形态
/// </summary>
/// <remarks>
/// <c>users.create / get / update</c> 的响应与登录响应里的资料共用；<c>users.list</c> 的列表项是 <see cref="UserListItemResponse"/>
/// </remarks>
/// <param name="Id">用户 ID</param>
/// <param name="UserName">登录名</param>
/// <param name="DisplayName">显示名</param>
/// <param name="Email">邮箱</param>
/// <param name="PhoneNumber">手机号</param>
/// <param name="HomeTenantId">注册地租户 ID；平台账号为 <c>platform</c></param>
/// <param name="Status">状态</param>
/// <param name="LastLoginAt">最近登录时间（UTC）</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="CreatedBy">创建人（主体 ID）</param>
/// <param name="AvatarFileId">头像文件（前端按它换签名地址显示）；没设为空</param>
/// <param name="IsSystemAccount">是否系统内置账号：不能停用、删除、锁定或由管理员重置密码</param>
/// <param name="RealName">真实姓名</param>
/// <param name="Gender">性别</param>
/// <param name="Birthday">生日</param>
/// <param name="Country">国家或地区</param>
/// <param name="Remark">备注（本人在个人中心看到的个人简介是同一列）</param>
/// <param name="LastLoginIp">最近一次登录的 IP</param>
/// <param name="IsExternalMember">是否外部成员：注册在别处、以成员身份加入当前租户，账号由注册地维护。只有用户管理的查询（<c>users.list / get / detail</c>）判定，其余响应为假</param>
/// <param name="IsSupportMember">是否运维支持成员：平台账号以运维支持身份进入当前租户，账号由平台维护、只能配角色（这时 <c>IsExternalMember</c> 也为真）。只有用户管理的查询判定，其余响应为假</param>
public sealed record UserResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    string HomeTenantId,
    UserStatus Status,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    Guid? AvatarFileId,
    bool IsSystemAccount,
    string? RealName,
    UserGender Gender,
    DateTimeOffset? Birthday,
    string? Country,
    string? Remark,
    string? LastLoginIp,
    bool IsExternalMember,
    bool IsSupportMember)
{
    /// <summary>
    /// 由实体投影
    /// </summary>
    public static UserResponse From(SysUser user) => From(user, null);

    /// <summary>
    /// 由实体投影，并按调用方所在的租户判定是不是外部成员
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="tenantId">调用方所在的租户；平台一侧为空</param>
    /// <param name="isSupportMember">是不是调用方所在租户的运维支持成员</param>
    public static UserResponse From(SysUser user, string? tenantId, bool isSupportMember = false)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserResponse(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email,
            user.PhoneNumber,
            user.HomeTenantId,
            user.Status,
            user.LastLoginAt,
            user.CreatedAt,
            user.CreatedBy,
            user.AvatarFileId,
            user.IsSystemAccount,
            user.RealName,
            user.Gender,
            user.Birthday,
            user.Country,
            user.Remark,
            user.LastLoginIp,
            tenantId is not null && !string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal),
            isSupportMember);
    }
}
