// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 角色成员目录（Access 实现）。
/// </summary>
/// <remarks>
/// 别的包按角色编码取调用所在一侧的角色成员，以及选人用的角色清单；不做权限判定。
/// </remarks>
public interface IRoleMemberDirectory
{
    /// <summary>
    /// 角色成员。
    /// </summary>
    /// <remarks>
    /// 只取当前一侧启用的角色、此刻在有效期内的指派；按编码顺序、成员去重。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="roleCodes">角色编码。</param>
    /// <returns>成员的用户 ID。</returns>
    Task<IReadOnlyList<Guid>> MembersAsync(InvocationContext context, IReadOnlyCollection<string> roleCodes);

    /// <summary>
    /// 启用的角色。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>当前一侧按排序的角色清单。</returns>
    Task<IReadOnlyList<RoleOption>> RolesAsync(InvocationContext context);
}

/// <summary>
/// 选人用的角色。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
public sealed record RoleOption(Guid Id, string Code, string Name);
