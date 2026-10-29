// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 见方案 5.9。用户目录（<c>users.list / get / create / update</c>）在投影、查询与写入处套用规则；
/// 登录、刷新、个人中心等本人自己的资料不套用。字段名与响应、筛选、排序的字段同名。
/// </remarks>
public static class IdentityFieldSecurity
{
    /// <summary>
    /// 用户实体码。
    /// </summary>
    public const string Users = "identity.users";

    /// <summary>
    /// 登录名。
    /// </summary>
    public const string UserName = "userName";

    /// <summary>
    /// 显示名。
    /// </summary>
    public const string DisplayName = "displayName";

    /// <summary>
    /// 邮箱。
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// 手机号。
    /// </summary>
    public const string PhoneNumber = "phoneNumber";

    /// <summary>
    /// 最近登录时间。
    /// </summary>
    public const string LastLoginAt = "lastLoginAt";

    /// <summary>
    /// 全部实体。
    /// </summary>
    /// <remarks>
    /// 显示名是前端用户页已有的文案键。
    /// </remarks>
    public static IReadOnlyList<FieldSecurityEntityDescriptor> All { get; } =
    [
        new(Users, "identity.users.title",
        [
            new(UserName, "identity.users.fields.user_name", IsText: true, IsNullable: false),
            new(DisplayName, "identity.users.fields.display_name", IsText: true, IsNullable: false),
            new(Email, "identity.users.fields.email", IsText: true, IsNullable: true),
            new(PhoneNumber, "identity.users.fields.phone", IsText: true, IsNullable: true),
            new(LastLoginAt, "identity.users.fields.last_login_at", IsText: false, IsNullable: true),
        ]),
    ];
}
