#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantUserRepository
// Guid:4260df49-6042-4c3f-bb40-a959b484feee
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 租户成员仓储接口
/// </summary>
public interface ITenantUserRepository : ISaasRepository<SysTenantUser>
{
    /// <summary>
    /// 获取用户可进入的租户成员关系
    /// </summary>
    Task<IReadOnlyList<SysTenantUser>> GetActiveByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定租户成员关系
    /// </summary>
    Task<SysTenantUser?> GetMembershipAsync(long userId, CancellationToken cancellationToken = default);
}
