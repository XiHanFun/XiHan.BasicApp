// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的权限码：<c>identity.&lt;资源&gt;.&lt;动作&gt;</c>。操作的 <c>Permission</c> 引用这里的常量，权限目录种子从 <see cref="All"/> 派生。
/// </summary>
public static class IdentityPermissions
{
    /// <summary>读用户：<c>users.get</c>、<c>users.list</c>。</summary>
    public const string UsersRead = "identity.users.read";

    /// <summary>创建用户：<c>users.create</c>。</summary>
    public const string UsersCreate = "identity.users.create";

    /// <summary>
    /// 本包全部权限的描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(UsersRead, "Read users", PermissionScope.Both, description: "View users of the current tenant."),
        new(UsersCreate, "Create users", PermissionScope.Both, audited: true, description: "Create users in the current tenant."),
    ];
}
