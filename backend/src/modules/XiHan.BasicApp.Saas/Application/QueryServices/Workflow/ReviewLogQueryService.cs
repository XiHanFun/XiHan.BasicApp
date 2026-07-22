// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 审查日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "审查日志")]
public sealed class ReviewLogQueryService
    : SaasApplicationService, IReviewLogQueryService
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewLogQueryService(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 获取审查日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.ReviewLog.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<ReviewLogListItemDto>> GetReviewLogPageAsync(ReviewLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var predicate = BuildReviewLogPredicate(input);
        RefAsync<int> totalCount = 0;
        var entities = await DbClient.Queryable<SysReviewLog>()
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.ReviewTime)
            .ToPageListAsync(input.Page.PageIndex, input.Page.PageSize, totalCount, cancellationToken);

        var page = new PageResultMetadata(input.Page.PageIndex, input.Page.PageSize, totalCount);
        if (entities.Count == 0)
        {
            return new PageResultDtoBase<ReviewLogListItemDto>([], page);
        }

        var items = entities
            .Select(ReviewLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<ReviewLogListItemDto>(items, page);
    }

    /// <summary>
    /// 获取审查日志详情
    /// </summary>
    /// <param name="id">审查日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.ReviewLog.Read)]
    public async Task<ReviewLogDetailDto?> GetReviewLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "审查日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var reviewLog = await DbClient.Queryable<SysReviewLog>()
            .Where(x => x.BasicId == id)
            .SplitTable()
            .FirstAsync(cancellationToken);
        return reviewLog is null ? null : ReviewLogApplicationMapper.ToDetailDto(reviewLog);
    }

    /// <summary>
    /// 构建审查日志查询表达式
    /// </summary>
    private static Expression<Func<SysReviewLog, bool>> BuildReviewLogPredicate(ReviewLogPageQueryDto input)
    {
        Expression<Func<SysReviewLog, bool>> predicate = reviewLog => true;

        if (input.ReviewTimeStart.HasValue)
        {
            var beginTime = input.ReviewTimeStart.Value;
            predicate = And(predicate, reviewLog => reviewLog.ReviewTime >= beginTime);
        }

        if (input.ReviewTimeEnd.HasValue)
        {
            var endTime = input.ReviewTimeEnd.Value;
            predicate = And(predicate, reviewLog => reviewLog.ReviewTime <= endTime);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, reviewLog => reviewLog.ReviewAction != null && reviewLog.ReviewAction.Contains(keyword));
        }

        if (input.ReviewId.HasValue)
        {
            var reviewId = input.ReviewId.Value;
            predicate = And(predicate, reviewLog => reviewLog.ReviewId == reviewId);
        }

        if (input.ReviewLevel.HasValue)
        {
            var reviewLevel = input.ReviewLevel.Value;
            predicate = And(predicate, reviewLog => reviewLog.ReviewLevel == reviewLevel);
        }

        if (input.ReviewUserId.HasValue)
        {
            var reviewUserId = input.ReviewUserId.Value;
            predicate = And(predicate, reviewLog => reviewLog.ReviewUserId == reviewUserId);
        }

        if (input.OriginalStatus.HasValue)
        {
            var originalStatus = input.OriginalStatus.Value;
            predicate = And(predicate, reviewLog => reviewLog.OriginalStatus == originalStatus);
        }

        if (input.NewStatus.HasValue)
        {
            var newStatus = input.NewStatus.Value;
            predicate = And(predicate, reviewLog => reviewLog.NewStatus == newStatus);
        }

        if (input.ReviewResult.HasValue)
        {
            var reviewResult = input.ReviewResult.Value;
            predicate = And(predicate, reviewLog => reviewLog.ReviewResult == reviewResult);
        }

        if (!string.IsNullOrWhiteSpace(input.ReviewAction))
        {
            var reviewAction = input.ReviewAction.Trim();
            predicate = And(predicate, reviewLog => reviewLog.ReviewAction == reviewAction);
        }

        return predicate;
    }

    /// <summary>
    /// 合并查询表达式
    /// </summary>
    private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(ReviewLogPageQueryDto input)
    {
        if (input.ReviewTimeStart.HasValue && input.ReviewTimeEnd.HasValue &&
            input.ReviewTimeStart.Value > input.ReviewTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.ReviewTimeStart), "审查开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.ReviewId, nameof(input.ReviewId), "审查主键必须大于 0。");
        ValidateOptionalId(input.ReviewUserId, nameof(input.ReviewUserId), "审查人主键必须大于 0。");
        ValidateReviewLevel(input.ReviewLevel);
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.ReviewAction, 50, nameof(input.ReviewAction), "审查动作长度不能超过 50。");

        if (input.OriginalStatus.HasValue && !Enum.IsDefined(input.OriginalStatus.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.OriginalStatus), "原审查状态无效。");
        }

        if (input.NewStatus.HasValue && !Enum.IsDefined(input.NewStatus.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.NewStatus), "新审查状态无效。");
        }

        if (input.ReviewResult.HasValue && !Enum.IsDefined(input.ReviewResult.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.ReviewResult), "审查结果无效。");
        }
    }

    /// <summary>
    /// 校验可空主键
    /// </summary>
    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验审查级别
    /// </summary>
    private static void ValidateReviewLevel(int? reviewLevel)
    {
        if (reviewLevel is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reviewLevel), "审查级别必须大于 0。");
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateMaxLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 替换表达式参数
    /// </summary>
    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : node;
        }
    }
}
