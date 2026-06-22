#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISuperAdminProtector
// Guid:7c1f5a30-6e84-4b21-9d4c-2a0f7e3b8d61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/22 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 超级管理员保护守卫。
/// </summary>
/// <remarks>
/// 共享守卫，供各 AppService 调用以防护 <c>super_admin</c> 角色与超管用户：
/// <list type="bullet">
/// <item>非超管用户：对 super_admin 角色与超管用户的一切写操作（增删改启停/改密/角色授予撤销/权限授予撤销/数据范围/继承）一律拒绝。</item>
/// <item>非超管用户：列表/选择项应隐藏受保护的角色与用户（由调用方据 <see cref="GetProtectedRoleIdsAsync"/>/<see cref="GetProtectedUserIdsAsync"/> 过滤）。</item>
/// <item>超管自身不受限（能看能管）。</item>
/// </list>
/// 保留 super_admin 角色，不删除。
/// </remarks>
public interface ISuperAdminProtector
{
    /// <summary>
    /// 当前用户是否为超级管理员（持有 <c>super_admin</c> 角色）。
    /// </summary>
    bool IsCurrentUserSuperAdmin();

    /// <summary>
    /// 获取受保护角色 id 集合（RoleCode == <c>super_admin</c> 的角色）。
    /// </summary>
    Task<IReadOnlyCollection<long>> GetProtectedRoleIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取受保护用户 id 集合（持有受保护角色、且授权有效的用户）。
    /// </summary>
    Task<IReadOnlyCollection<long>> GetProtectedUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定角色是否受保护（是否为 <c>super_admin</c> 角色）。
    /// </summary>
    Task<bool> IsProtectedRoleAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定用户是否受保护（是否为超管用户）。
    /// </summary>
    Task<bool> IsProtectedUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验当前用户可对指定角色执行写操作；非超管且该角色受保护时抛出禁止异常。
    /// </summary>
    Task EnsureCanWriteRoleAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验当前用户可对指定用户执行写操作；非超管且该用户受保护时抛出禁止异常。
    /// </summary>
    Task EnsureCanWriteUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验当前用户可对指定角色执行授予/撤销操作；非超管且该角色为 <c>super_admin</c> 时抛出禁止异常。
    /// </summary>
    Task EnsureCanAssignRoleAsync(long roleId, CancellationToken cancellationToken = default);
}
