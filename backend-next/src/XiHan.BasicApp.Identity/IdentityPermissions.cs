// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的权限码。
/// </summary>
/// <remarks>
/// 格式为 <c>identity.&lt;资源&gt;.&lt;动作&gt;</c>。操作的 <c>Permission</c> 引用这里的常量，权限目录种子从 <see cref="All"/> 派生。
/// </remarks>
public static class IdentityPermissions
{
    /// <summary>
    /// 读用户：<c>users.get</c>、<c>users.list</c>。
    /// </summary>
    public const string UsersRead = "identity.users.read";

    /// <summary>
    /// 创建用户：<c>users.create</c>。
    /// </summary>
    public const string UsersCreate = "identity.users.create";

    /// <summary>
    /// 修改用户资料：<c>users.update</c>。
    /// </summary>
    public const string UsersUpdate = "identity.users.update";

    /// <summary>
    /// 删除用户：<c>users.delete</c>。
    /// </summary>
    public const string UsersDelete = "identity.users.delete";

    /// <summary>
    /// 锁定与解锁用户：<c>users.lock</c>、<c>users.unlock</c>。
    /// </summary>
    public const string UsersLock = "identity.users.lock";

    /// <summary>
    /// 重置用户密码：<c>users.reset-password</c>。
    /// </summary>
    public const string UsersResetPassword = "identity.users.reset-password";

    /// <summary>
    /// 重置用户的双因素：<c>users.reset-two-factor</c>。
    /// </summary>
    public const string UsersResetTwoFactor = "identity.users.reset-two-factor";

    /// <summary>
    /// 模仿登录。
    /// </summary>
    /// <remarks>
    /// 用于 <c>impersonation.start</c>、<c>impersonation.candidates</c>（结束模仿是自助操作）。
    /// </remarks>
    public const string ImpersonationStart = "identity.impersonation.start";

    /// <summary>
    /// 查看在线用户：<c>sessions.list</c>、<c>sessions.summary</c>。
    /// </summary>
    public const string SessionsRead = "identity.sessions.read";

    /// <summary>
    /// 强制下线：<c>sessions.revoke</c>。
    /// </summary>
    public const string SessionsRevoke = "identity.sessions.revoke";

    /// <summary>
    /// 本包全部权限的描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(UsersRead, "Read users", PermissionScope.Both, description: "View users of the current tenant."),
        new(UsersCreate, "Create users", PermissionScope.Both, audited: true, description: "Create users in the current tenant."),
        new(UsersUpdate, "Update users", PermissionScope.Both, audited: true, description: "Edit user profiles of the current tenant."),
        new(UsersDelete, "Delete users", PermissionScope.Both, audited: true, description: "Delete users of the current tenant."),
        new(UsersLock, "Lock users", PermissionScope.Both, audited: true, description: "Lock and unlock users of the current tenant."),
        new(UsersResetPassword, "Reset passwords", PermissionScope.Both, audited: true, description: "Reset user passwords; the user must change the password at next sign-in."),
        new(UsersResetTwoFactor, "Reset two-factor", PermissionScope.Both, audited: true, description: "Clear a user's two-factor methods and authenticator key so they can sign in with the password and enroll again."),
        new(ImpersonationStart, "Impersonate users", PermissionScope.Both, audited: true, description: "Sign in as another user of the current tenant (platform: any tenant) to troubleshoot on their behalf."),
        new(SessionsRead, "Read online users", PermissionScope.Both, description: "View the active sessions of the current tenant with device, address and realtime presence."),
        new(SessionsRevoke, "Force sign-out", PermissionScope.Both, audited: true, description: "Revoke active sessions of the current tenant; the signed-out clients are notified in real time."),
    ];
}
