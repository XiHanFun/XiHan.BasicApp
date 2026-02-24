#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOperationLogRepository
// Guid:82042ec2-b3ff-4791-ae65-d2e77805c7d6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:31:20
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 操作日志仓储接口
/// </summary>
public interface ISysOperationLogRepository : IReadOnlyRepositoryBase<SysOperationLog, long>
{
    /// <summary>
    /// 保存操作日志
    /// </summary>
    /// <param name="log">日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存后的实体</returns>
    Task<SysOperationLog> SaveAsync(SysOperationLog log, CancellationToken cancellationToken = default);
}
