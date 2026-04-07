#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskLogRepository
// Guid:6f810f7f-c83d-4e65-a56f-5df811f14913
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 11:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 任务日志仓储接口
/// </summary>
public interface ITaskLogRepository : IRepositoryBase<SysTaskLog, long>
{
    /// <summary>
    /// 清空任务日志
    /// </summary>
    Task<bool> ClearAsync(CancellationToken cancellationToken = default);
}
