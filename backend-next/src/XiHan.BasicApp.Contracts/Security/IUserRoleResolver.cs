// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 解析用户在某个租户侧的角色。
/// </summary>
/// <remarks>
/// 角色进入访问令牌的 <c>role</c> 声明，值是角色 ID；框架授权按 <c>role:&lt;ID&gt;</c> 找授予。
/// 由 Access 包实现、Identity 在登录与刷新时消费；没有实现时令牌不带角色。
/// </remarks>
public interface IUserRoleResolver
{
    /// <summary>
    /// 用户在 <paramref name="tenantId"/>（<see langword="null"/> 即平台侧）生效的角色 ID。
    /// </summary>
    Task<IReadOnlyList<string>> ResolveAsync(Guid userId, string? tenantId, CancellationToken cancellationToken);
}
