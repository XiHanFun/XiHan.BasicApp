#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRepository
// Guid:66af7d1e-c9c4-4d8d-913f-2bb82edab1bf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : ISaasAggregateRepository<SysUser>
{
    /// <summary>
    /// 根据当前租户和用户名获取用户
    /// </summary>
    Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查当前租户下用户名是否存在
    /// </summary>
    Task<bool> ExistsUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default);
}
