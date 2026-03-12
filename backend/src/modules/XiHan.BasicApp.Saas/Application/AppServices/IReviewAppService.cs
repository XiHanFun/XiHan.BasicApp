#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewAppService
// Guid:ac3c95f1-2a0d-43b2-ac5c-b6490525e8ca
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:41:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 审查应用服务
/// </summary>
public interface IReviewAppService
    : ICrudApplicationService<SysReview, ReviewDto, long, ReviewCreateDto, ReviewUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据审查编码获取审查
    /// </summary>
    Task<ReviewDto?> GetByReviewCodeAsync(string reviewCode, long? tenantId = null);
}
