#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleResolutionDomainService
// Guid:d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色解析领域服务
/// </summary>
public interface IRoleResolutionDomainService
{
    /// <summary>
    /// 获取用户有效角色编码（含继承展开）
    /// </summary>
    Task<IReadOnlyList<string>> GetUserRoleCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户有效角色ID（含继承展开）
    /// </summary>
    Task<IReadOnlyList<long>> GetEffectiveRoleIdsAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);
}
