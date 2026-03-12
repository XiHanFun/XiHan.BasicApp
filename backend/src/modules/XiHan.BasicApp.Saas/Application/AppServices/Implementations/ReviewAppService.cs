#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewAppService
// Guid:ea2bf2d0-5d75-4f9e-b001-c6f96e657d78
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:57:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 审查应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class ReviewAppService
    : CrudApplicationServiceBase<SysReview, ReviewDto, long, ReviewCreateDto, ReviewUpdateDto, BasicAppPRDto>,
        IReviewAppService
{
    private readonly IReviewRepository _reviewRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewAppService(IReviewRepository reviewRepository)
        : base(reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    /// <summary>
    /// 根据审查编码获取审查
    /// </summary>
    public async Task<ReviewDto?> GetByReviewCodeAsync(string reviewCode, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewCode);
        var entity = await _reviewRepository.GetByReviewCodeAsync(reviewCode, tenantId);
        return entity?.Adapt<ReviewDto>();
    }

    /// <summary>
    /// 创建审查
    /// </summary>
    public override async Task<ReviewDto> CreateAsync(ReviewCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.ReviewCode.Trim();
        var exists = await _reviewRepository.IsReviewCodeExistsAsync(normalizedCode, input.TenantId);
        if (exists)
        {
            throw new BusinessException(message: $"审查编码 '{normalizedCode}' 已存在");
        }

        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新审查
    /// </summary>
    public override async Task<ReviewDto> UpdateAsync(ReviewUpdateDto input)
    {
        input.ValidateAnnotations();
        return await base.UpdateAsync(input);
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysReview> MapDtoToEntityAsync(ReviewCreateDto createDto)
    {
        var entity = new SysReview
        {
            TenantId = createDto.TenantId,
            ReviewCode = createDto.ReviewCode.Trim(),
            ReviewTitle = createDto.ReviewTitle.Trim(),
            ReviewType = createDto.ReviewType.Trim(),
            ReviewContent = createDto.ReviewContent,
            ReviewDescription = createDto.ReviewDescription,
            EntityType = createDto.EntityType,
            EntityId = createDto.EntityId,
            BusinessData = createDto.BusinessData,
            Priority = createDto.Priority,
            SubmitUserId = createDto.SubmitUserId,
            SubmitUserName = createDto.SubmitUserName,
            SubmitTime = createDto.SubmitTime,
            CurrentReviewUserId = createDto.CurrentReviewUserId,
            CurrentReviewUserName = createDto.CurrentReviewUserName,
            ReviewUserIds = createDto.ReviewUserIds,
            ReviewLevel = createDto.ReviewLevel,
            CurrentLevel = createDto.CurrentLevel,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(ReviewUpdateDto updateDto, SysReview entity)
    {
        entity.ReviewTitle = updateDto.ReviewTitle.Trim();
        entity.ReviewType = updateDto.ReviewType.Trim();
        entity.ReviewContent = updateDto.ReviewContent;
        entity.ReviewDescription = updateDto.ReviewDescription;
        entity.EntityType = updateDto.EntityType;
        entity.EntityId = updateDto.EntityId;
        entity.BusinessData = updateDto.BusinessData;
        entity.ReviewStatus = updateDto.ReviewStatus;
        entity.ReviewResult = updateDto.ReviewResult;
        entity.Priority = updateDto.Priority;
        entity.CurrentReviewUserId = updateDto.CurrentReviewUserId;
        entity.CurrentReviewUserName = updateDto.CurrentReviewUserName;
        entity.ReviewUserIds = updateDto.ReviewUserIds;
        entity.ReviewLevel = updateDto.ReviewLevel;
        entity.CurrentLevel = updateDto.CurrentLevel;
        entity.ReviewComment = updateDto.ReviewComment;
        entity.ReviewStartTime = updateDto.ReviewStartTime;
        entity.ReviewEndTime = updateDto.ReviewEndTime;
        entity.Status = updateDto.Status;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }
}
