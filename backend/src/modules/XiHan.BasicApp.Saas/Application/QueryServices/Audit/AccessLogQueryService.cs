#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogQueryService
// Guid:5dba2cc7-8dd5-4de3-9f87-e9bc0db612e8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 访问日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "访问日志")]
public sealed class AccessLogQueryService(IAccessLogRepository accessLogRepository)
    : SaasApplicationService, IAccessLogQueryService
{
    /// <summary>
    /// 访问日志仓储
    /// </summary>
    private readonly IAccessLogRepository _accessLogRepository = accessLogRepository;

    /// <summary>
    /// 获取访问日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.AccessLog.Read)]
    public async Task<PageResultDtoBase<AccessLogListItemDto>> GetAccessLogPageAsync(AccessLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.AccessTimeStart!.Value;
        var endTime = input.AccessTimeEnd!.Value;
        var predicate = BuildAccessLogPredicate(input, beginTime, endTime);
        var accessLogPage = await _accessLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            accessLog => accessLog.AccessTime,
            false,
            cancellationToken);

        if (accessLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<AccessLogListItemDto>([], accessLogPage.Page);
        }

        var items = accessLogPage.Items
            .Select(AccessLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<AccessLogListItemDto>(items, accessLogPage.Page);
    }

    /// <summary>
    /// 获取访问日志详情
    /// </summary>
    /// <param name="id">访问日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.AccessLog.Read)]
    public async Task<AccessLogDetailDto?> GetAccessLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "访问日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var accessLog = await _accessLogRepository.GetByIdAsync(id, cancellationToken);
        return accessLog is null ? null : AccessLogApplicationMapper.ToDetailDto(accessLog);
    }

    /// <summary>
    /// 构建访问日志查询表达式
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="beginTime">起始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>访问日志查询表达式</returns>
    private static Expression<Func<SysAccessLog, bool>> BuildAccessLogPredicate(AccessLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysAccessLog, bool>> predicate = accessLog => accessLog.AccessTime >= beginTime && accessLog.AccessTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, accessLog =>
                (accessLog.UserName != null && accessLog.UserName.Contains(keyword)) ||
                (accessLog.SessionId != null && accessLog.SessionId.Contains(keyword)) ||
                (accessLog.TraceId != null && accessLog.TraceId.Contains(keyword)) ||
                accessLog.ResourcePath.Contains(keyword) ||
                (accessLog.ResourceName != null && accessLog.ResourceName.Contains(keyword)) ||
                (accessLog.ResourceType != null && accessLog.ResourceType.Contains(keyword)) ||
                (accessLog.Method != null && accessLog.Method.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, accessLog => accessLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, accessLog => accessLog.UserName != null && accessLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, accessLog => accessLog.SessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, accessLog => accessLog.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(input.ResourcePath))
        {
            var resourcePath = input.ResourcePath.Trim();
            predicate = And(predicate, accessLog => accessLog.ResourcePath.Contains(resourcePath));
        }

        if (!string.IsNullOrWhiteSpace(input.ResourceType))
        {
            var resourceType = input.ResourceType.Trim();
            predicate = And(predicate, accessLog => accessLog.ResourceType == resourceType);
        }

        if (!string.IsNullOrWhiteSpace(input.Method))
        {
            var method = input.Method.Trim().ToUpperInvariant();
            predicate = And(predicate, accessLog => accessLog.Method == method);
        }

        if (input.AccessResult.HasValue)
        {
            var accessResult = input.AccessResult.Value;
            predicate = And(predicate, accessLog => accessLog.AccessResult == accessResult);
        }

        if (input.StatusCode.HasValue)
        {
            var statusCode = input.StatusCode.Value;
            predicate = And(predicate, accessLog => accessLog.StatusCode == statusCode);
        }

        if (input.MinExecutionTime.HasValue)
        {
            var minExecutionTime = input.MinExecutionTime.Value;
            predicate = And(predicate, accessLog => accessLog.ExecutionTime >= minExecutionTime);
        }

        if (input.MaxExecutionTime.HasValue)
        {
            var maxExecutionTime = input.MaxExecutionTime.Value;
            predicate = And(predicate, accessLog => accessLog.ExecutionTime <= maxExecutionTime);
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
    private static void ValidatePageInput(AccessLogPageQueryDto input)
    {
        if (!input.AccessTimeStart.HasValue || !input.AccessTimeEnd.HasValue)
        {
            throw new ArgumentException("访问日志分表查询必须提供访问开始时间和访问结束时间。", nameof(input));
        }

        if (input.AccessTimeStart.Value > input.AccessTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.AccessTimeStart), "访问开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.UserName, 50, nameof(input.UserName), "用户名长度不能超过 50。");
        ValidateMaxLength(input.SessionId, 100, nameof(input.SessionId), "会话标识长度不能超过 100。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");
        ValidateMaxLength(input.ResourcePath, 500, nameof(input.ResourcePath), "资源路径长度不能超过 500。");
        ValidateMaxLength(input.ResourceType, 50, nameof(input.ResourceType), "资源类型长度不能超过 50。");
        ValidateMaxLength(input.Method, 10, nameof(input.Method), "请求方法长度不能超过 10。");
        ValidateStatusCode(input.StatusCode);
        ValidateExecutionTime(input.MinExecutionTime, nameof(input.MinExecutionTime), "最小执行耗时不能小于 0。");
        ValidateExecutionTime(input.MaxExecutionTime, nameof(input.MaxExecutionTime), "最大执行耗时不能小于 0。");

        if (input.AccessResult.HasValue && !Enum.IsDefined(input.AccessResult.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.AccessResult), "访问结果无效。");
        }

        if (input.MinExecutionTime.HasValue &&
            input.MaxExecutionTime.HasValue &&
            input.MinExecutionTime.Value > input.MaxExecutionTime.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.MinExecutionTime), "最小执行耗时不能大于最大执行耗时。");
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
    /// 校验状态码
    /// </summary>
    private static void ValidateStatusCode(int? statusCode)
    {
        if (statusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), "响应状态码必须在 100 到 599 之间。");
        }
    }

    /// <summary>
    /// 校验执行耗时
    /// </summary>
    private static void ValidateExecutionTime(long? executionTime, string paramName, string message)
    {
        if (executionTime is < 0)
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
