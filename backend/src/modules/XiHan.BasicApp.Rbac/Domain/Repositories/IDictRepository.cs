#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictRepository
// Guid:cc4fc63c-2819-46e2-b0e4-8f86f58ab8d7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:36:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 平台字典仓储接口
/// </summary>
public interface IDictRepository : IAggregateRootRepository<SysDict, long>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    Task<SysDict?> GetByDictCodeAsync(string dictCode, long? tenantId = null, CancellationToken cancellationToken = default);
}
