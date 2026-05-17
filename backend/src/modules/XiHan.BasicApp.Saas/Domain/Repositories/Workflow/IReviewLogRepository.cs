#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewLogRepository
// Guid:396c3cf2-22de-41d4-8060-8c8b7dfb1bde
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 审查日志仓储接口
/// </summary>
public interface IReviewLogRepository : ISaasRepository<SysReviewLog>
{
    /// <summary>
    /// 添加审查日志
    /// </summary>
    Task AddLogAsync(SysReviewLog reviewLog, CancellationToken cancellationToken = default);
}
