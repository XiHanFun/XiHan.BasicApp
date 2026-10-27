// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Operations.Users;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.me</c> 的响应：当前主体的资料、租户、会话与角色（权限码与菜单随 Access 包的 <c>auth.permissions</c> 下发）。
/// </summary>
/// <param name="User">用户资料。</param>
/// <param name="TenantId">当前租户；平台账号为 <see langword="null"/>。</param>
/// <param name="SessionId">会话 ID；非本应用签发的令牌没有。</param>
/// <param name="Roles">角色（令牌里的 <c>role</c> 声明）。</param>
/// <param name="MustChangePassword">是否必须先改密。</param>
/// <param name="TenantName">当前租户的显示名；平台为 <see langword="null"/>。</param>
/// <param name="ImpersonatorUserName">模仿会话的发起人登录名；普通会话为 <see langword="null"/>。</param>
public sealed record MeResponse(UserResponse User, string? TenantId, Guid? SessionId, IReadOnlyList<string> Roles, bool MustChangePassword, string? TenantName = null, string? ImpersonatorUserName = null);
