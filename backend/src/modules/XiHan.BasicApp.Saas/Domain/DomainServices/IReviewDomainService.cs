#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewDomainService
// Guid:7f809102-1324-3456-f012-3456789abc03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 审核领域服务接口
/// </summary>
public interface IReviewDomainService
{
    /// <summary>
    /// 创建审核
    /// </summary>
    Task<SysReview> CreateAsync(SysReview review);

    /// <summary>
    /// 更新审核
    /// </summary>
    Task<SysReview> UpdateAsync(SysReview review);

    /// <summary>
    /// 删除审核
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
