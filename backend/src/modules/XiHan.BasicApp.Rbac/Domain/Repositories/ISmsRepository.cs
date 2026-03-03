#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsRepository
// Guid:4e3cc8f6-ac2c-4d3c-8ca4-f9ccce2cb9d2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:09:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 短信仓储接口
/// </summary>
public interface ISmsRepository : IAggregateRootRepository<SysSms, long>
{
    /// <summary>
    /// 获取待发送短信
    /// </summary>
    Task<IReadOnlyList<SysSms>> GetPendingSmsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default);
}
