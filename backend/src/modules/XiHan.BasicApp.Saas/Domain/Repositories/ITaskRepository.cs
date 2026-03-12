#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskRepository
// Guid:2e7041af-d947-4314-8e39-6637d0d5f0f9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 任务仓储接口
/// </summary>
public interface ITaskRepository : IAggregateRootRepository<SysTask, long>
{
    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    Task<SysTask?> GetByTaskCodeAsync(string taskCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验任务编码是否已存在
    /// </summary>
    Task<bool> IsTaskCodeExistsAsync(string taskCode, long? tenantId = null, long? excludeTaskId = null, CancellationToken cancellationToken = default);
}
