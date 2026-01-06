#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysSessionRoleRepository
// Guid:7a2b3c4d-5e6f-7890-abcd-ef1234567806
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.SessionRoles;

/// <summary>
/// 系统会话角色仓储接口
/// </summary>
public interface ISysSessionRoleRepository : IRepositoryBase<SysSessionRole, long>
{
    /// <summary>
    /// 获取会话中的活跃角色列表
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task<List<SysSessionRole>> GetActiveRolesAsync(long sessionId);

    /// <summary>
    /// 获取会话中的所有角色（包括已停用）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task<List<SysSessionRole>> GetAllSessionRolesAsync(long sessionId);

    /// <summary>
    /// 激活会话角色
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="roleId">角色ID</param>
    Task ActivateRoleAsync(long sessionId, long roleId);

    /// <summary>
    /// 停用会话角色
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="roleId">角色ID</param>
    Task DeactivateRoleAsync(long sessionId, long roleId);

    /// <summary>
    /// 检查角色是否在会话中激活
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<bool> IsRoleActiveAsync(long sessionId, long roleId);

    /// <summary>
    /// 获取会话中激活的角色ID列表
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task<List<long>> GetActiveRoleIdsAsync(long sessionId);

    /// <summary>
    /// 清除会话中的所有角色
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    Task ClearSessionRolesAsync(long sessionId);
}
