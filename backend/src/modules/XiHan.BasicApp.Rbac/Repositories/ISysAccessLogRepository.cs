#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAccessLogRepository
// Guid:9f504f7e-7a14-4a15-bd13-66673dd8b0fb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:31:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 访问日志仓储接口
/// </summary>
public interface ISysAccessLogRepository : IReadOnlyRepositoryBase<SysAccessLog, long>
{
    /// <summary>
    /// 保存访问日志
    /// </summary>
    /// <param name="log">日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存后的实体</returns>
    Task<SysAccessLog> SaveAsync(SysAccessLog log, CancellationToken cancellationToken = default);
}
