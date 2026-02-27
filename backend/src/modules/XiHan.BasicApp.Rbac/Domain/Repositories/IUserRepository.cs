#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRepository
// Guid:fd3374ae-4884-43ed-80f4-da3b1c02802d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:33:56
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户聚合仓储接口
/// </summary>
public interface IUserRepository : IAggregateRootRepository<SysUser, long>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验用户名是否已存在
    /// </summary>
    Task<bool> IsUserNameExistsAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
