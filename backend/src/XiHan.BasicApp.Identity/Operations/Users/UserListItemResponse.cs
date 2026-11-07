// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 用户列表的一行
/// </summary>
/// <remarks>
/// <see cref="UserResponse"/> 的字段，加上列表的角色、部门与安全标记：
/// 角色与部门来自 Access 与 Organization（经 Contracts 按这一页的用户批量取），只算调用所在的一侧
/// </remarks>
/// <param name="Id">用户 ID</param>
/// <param name="UserName">登录名</param>
/// <param name="DisplayName">显示名</param>
/// <param name="Email">邮箱</param>
/// <param name="PhoneNumber">手机号</param>
/// <param name="HomeTenantId">注册地租户 ID；平台账号为 <c>platform</c></param>
/// <param name="Status">状态</param>
/// <param name="LastLoginAt">最近登录时间（UTC）；运维支持成员在租户一侧为空</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="CreatedBy">创建人（主体 ID）</param>
/// <param name="AvatarFileId">头像文件（前端按它换签名地址显示）；没设为空</param>
/// <param name="IsSystemAccount">是否系统内置账号：不能停用、删除、锁定或由管理员重置密码</param>
/// <param name="RealName">真实姓名</param>
/// <param name="Gender">性别</param>
/// <param name="Birthday">生日</param>
/// <param name="Country">国家或地区</param>
/// <param name="Remark">备注</param>
/// <param name="LastLoginIp">最近一次登录的 IP；运维支持成员在租户一侧为空</param>
/// <param name="IsExternalMember">是否外部成员：注册在别处、以成员身份加入当前租户，账号由注册地维护</param>
/// <param name="IsSupportMember">是否运维支持成员：平台账号以运维支持身份进入当前租户，账号由平台维护、只能配角色（这时 <c>IsExternalMember</c> 也为真）</param>
/// <param name="RoleNames">当前一侧的角色名称（启用的角色、此刻在有效期内的指派）</param>
/// <param name="DepartmentName">主部门名称（没标主部门时取最早加入的）；平台一侧为空</param>
/// <param name="IsLocked">是否锁定：管理员锁定或登录失败锁定期内</param>
/// <param name="TwoFactorEnabled">是否开启了双因素</param>
public sealed record UserListItemResponse(
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
    bool IsSupportMember,
    IReadOnlyList<string> RoleNames,
    string? DepartmentName,
    bool IsLocked,
    bool TwoFactorEnabled)
{
    /// <summary>
    /// 由用户响应加上列表的列
    /// </summary>
    public static UserListItemResponse From(UserResponse user, IReadOnlyList<string> roleNames, string? departmentName, bool isLocked, bool twoFactorEnabled)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roleNames);
        return new UserListItemResponse(
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
            user.IsExternalMember,
            user.IsSupportMember,
            roleNames,
            departmentName,
            isLocked,
            twoFactorEnabled);
    }
}
