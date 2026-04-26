#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleRepository
// Guid:09f90250-8c8a-49c3-a345-96f68a199fa8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository : ISaasAggregateRepository<SysRole>
{
    /// <summary>
    /// 根据当前租户和角色编码获取角色
    /// </summary>
    Task<SysRole?> GetByCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取有效角色集合
    /// </summary>
    Task<IReadOnlyList<SysRole>> GetEnabledByIdsAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default);
}
