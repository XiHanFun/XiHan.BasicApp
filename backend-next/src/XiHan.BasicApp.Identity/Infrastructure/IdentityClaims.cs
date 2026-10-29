// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 本应用签发的 JWT 里的声明名。
/// </summary>
/// <remarks>
/// 见方案 5.8。框架 <c>Security.Jwt</c> 按 <c>sub</c> 认主体、<c>MultiTenancy</c> 按 <c>tenant</c> 解析租户、授权按 <c>role</c> 查授予。
/// </remarks>
public static class IdentityClaims
{
    /// <summary>
    /// 主体 ID（用户 ID）。
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// 显示名。
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// 登录名。
    /// </summary>
    public const string UserName = "preferred_username";

    /// <summary>
    /// 当前租户。
    /// </summary>
    /// <remarks>
    /// 平台账号没有。
    /// </remarks>
    public const string Tenant = "tenant";

    /// <summary>
    /// 会话 ID（<c>SysUserSession.Id</c>）。
    /// </summary>
    public const string Session = "sid";

    /// <summary>
    /// 角色。
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// 模仿者（发起模仿的用户 ID）。
    /// </summary>
    /// <remarks>
    /// 只有模仿会话的令牌带它，<c>impersonation.stop</c> 据它与会话行核对。
    /// </remarks>
    public const string Actor = "act";
}
