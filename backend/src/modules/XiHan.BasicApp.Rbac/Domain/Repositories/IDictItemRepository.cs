#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictItemRepository
// Guid:aa02c8f0-52fd-4076-8cd9-c32f366e0045
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:36:08
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 平台字典项仓储接口
/// </summary>
public interface IDictItemRepository : IRepositoryBase<SysDictItem, long>
{
    /// <summary>
    /// 获取字典项列表
    /// </summary>
    Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, long? tenantId = null, CancellationToken cancellationToken = default);
}
