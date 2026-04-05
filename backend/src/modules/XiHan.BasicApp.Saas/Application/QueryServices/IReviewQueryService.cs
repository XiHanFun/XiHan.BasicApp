#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewQueryService
// Guid:7f809102-1324-3456-f012-3456789abc01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 审核查询服务接口
/// </summary>
public interface IReviewQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取审核
    /// </summary>
    Task<ReviewDto?> GetByIdAsync(long id);
}
