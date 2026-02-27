#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSecurityRepository
// Guid:8d35fec7-b347-432f-ba7b-0559c4d42754
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:37
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户安全状态仓储接口
/// </summary>
public interface IUserSecurityRepository : IRepositoryBase<SysUserSecurity, long>
{
    /// <summary>
    /// 根据用户 ID 获取安全状态
    /// </summary>
    Task<SysUserSecurity?> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
