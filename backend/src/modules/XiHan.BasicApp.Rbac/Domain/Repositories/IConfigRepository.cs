#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigRepository
// Guid:eb1964dd-2096-4c9d-87f7-6ffba0174415
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:52
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 平台配置仓储接口
/// </summary>
public interface IConfigRepository : IAggregateRootRepository<SysConfig, long>
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default);
}
