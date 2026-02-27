#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleRepository
// Guid:e5251f7d-6ae5-4625-b5ca-fdb8ec2c8d96
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:34:03
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色聚合仓储接口
/// </summary>
public interface IRoleRepository : IAggregateRootRepository<SysRole, long>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验角色编码是否已存在
    /// </summary>
    Task<bool> IsRoleCodeExistsAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
