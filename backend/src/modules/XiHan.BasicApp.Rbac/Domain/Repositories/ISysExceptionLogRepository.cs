#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysExceptionLogRepository
// Guid:dabf1c2d-844e-499c-8fe0-c5498f1038b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:31:35
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 异常日志仓储接口
/// </summary>
public interface ISysExceptionLogRepository : IReadOnlyRepositoryBase<SysExceptionLog, long>
{
    /// <summary>
    /// 保存异常日志
    /// </summary>
    /// <param name="log">日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存后的实体</returns>
    Task<SysExceptionLog> SaveAsync(SysExceptionLog log, CancellationToken cancellationToken = default);
}
