#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAuditLogRepository
// Guid:b2cf15ba-7239-4bc4-b77b-f25bbdbf1380
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:31:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 审计日志仓储接口
/// </summary>
public interface ISysAuditLogRepository : IReadOnlyRepositoryBase<SysAuditLog, long>
{
    /// <summary>
    /// 保存审计日志
    /// </summary>
    /// <param name="log">日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存后的实体</returns>
    Task<SysAuditLog> SaveAsync(SysAuditLog log, CancellationToken cancellationToken = default);
}
