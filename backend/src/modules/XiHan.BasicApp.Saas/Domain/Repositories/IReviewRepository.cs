#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewRepository
// Guid:8f576afa-b4d2-463a-85a1-20f3b0fe8ca7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 审查仓储接口
/// </summary>
public interface IReviewRepository : IAggregateRootRepository<SysReview, long>
{
    /// <summary>
    /// 根据审查编码获取审查
    /// </summary>
    Task<SysReview?> GetByReviewCodeAsync(string reviewCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验审查编码是否已存在
    /// </summary>
    Task<bool> IsReviewCodeExistsAsync(string reviewCode, long? tenantId = null, long? excludeReviewId = null, CancellationToken cancellationToken = default);
}
