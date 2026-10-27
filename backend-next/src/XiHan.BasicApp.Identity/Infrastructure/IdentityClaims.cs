// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 本应用签发的 JWT 里的声明名（方案 5.8）：框架 <c>Security.Jwt</c> 按 <c>sub</c> 认主体、<c>MultiTenancy</c> 按 <c>tenant</c> 解析租户、授权按 <c>role</c> 查授予。
/// </summary>
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
    /// 当前租户；平台账号没有。
    /// </summary>
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
    /// 模仿者（真实主体）；N1 后续补。
    /// </summary>
    public const string Actor = "act";
}
