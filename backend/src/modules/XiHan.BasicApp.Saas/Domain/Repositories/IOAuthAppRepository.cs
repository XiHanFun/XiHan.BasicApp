#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppRepository
// Guid:f3ec7c8f-9f74-4346-88f8-93442f420487
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:11:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// OAuth应用仓储接口
/// </summary>
public interface IOAuthAppRepository : IAggregateRootRepository<SysOAuthApp, long>
{
    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    Task<SysOAuthApp?> GetByClientIdAsync(string clientId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验客户端ID是否已存在
    /// </summary>
    Task<bool> IsClientIdExistsAsync(string clientId, long? tenantId = null, long? excludeAppId = null, CancellationToken cancellationToken = default);
}
