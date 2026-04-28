#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISaasSplitRepository
// Guid:8d2cde9b-9a6b-4ef6-a7f0-d0122d0cf361
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// SaaS 分表实体仓储接口
/// </summary>
/// <typeparam name="TEntity">分表实体类型</typeparam>
public interface ISaasSplitRepository<TEntity> : ISplitRepositoryBase<TEntity>
    where TEntity : class, IEntityBase<long>, ISplitTableEntity, new()
{
}
