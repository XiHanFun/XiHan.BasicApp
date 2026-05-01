#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExceptionLogQueryService
// Guid:111330f9-f1ca-4389-a50e-81d60c3ddb2a
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
/// 异常日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "异常日志")]
public sealed class ExceptionLogQueryService(IExceptionLogRepository exceptionLogRepository)
    : SaasApplicationService, IExceptionLogQueryService
{
    /// <summary>
    /// 异常日志仓储
    /// </summary>
    private readonly IExceptionLogRepository _exceptionLogRepository = exceptionLogRepository;

    /// <summary>
    /// 获取异常日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.ExceptionLog.Read)]
    public async Task<PageResultDtoBase<ExceptionLogListItemDto>> GetExceptionLogPageAsync(ExceptionLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.ExceptionTimeStart!.Value;
        var endTime = input.ExceptionTimeEnd!.Value;
        var predicate = BuildExceptionLogPredicate(input, beginTime, endTime);
        var exceptionLogPage = await _exceptionLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            exceptionLog => exceptionLog.ExceptionTime,
            false,
            cancellationToken);

        if (exceptionLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<ExceptionLogListItemDto>([], exceptionLogPage.Page);
        }

        var items = exceptionLogPage.Items
            .Select(ExceptionLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<ExceptionLogListItemDto>(items, exceptionLogPage.Page);
    }

    /// <summary>
    /// 获取异常日志详情
    /// </summary>
    /// <param name="id">异常日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.ExceptionLog.Read)]
    public async Task<ExceptionLogDetailDto?> GetExceptionLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "异常日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var exceptionLog = await _exceptionLogRepository.GetByIdAsync(id, cancellationToken);
        return exceptionLog is null ? null : ExceptionLogApplicationMapper.ToDetailDto(exceptionLog);
    }

    /// <summary>
    /// 构建异常日志查询表达式
    /// </summary>
    private static Expression<Func<SysExceptionLog, bool>> BuildExceptionLogPredicate(ExceptionLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysExceptionLog, bool>> predicate = exceptionLog => exceptionLog.ExceptionTime >= beginTime && exceptionLog.ExceptionTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, exceptionLog =>
                (exceptionLog.UserName != null && exceptionLog.UserName.Contains(keyword)) ||
                (exceptionLog.SessionId != null && exceptionLog.SessionId.Contains(keyword)) ||
                (exceptionLog.RequestId != null && exceptionLog.RequestId.Contains(keyword)) ||
                (exceptionLog.TraceId != null && exceptionLog.TraceId.Contains(keyword)) ||
                exceptionLog.ExceptionType.Contains(keyword) ||
                (exceptionLog.ExceptionSource != null && exceptionLog.ExceptionSource.Contains(keyword)) ||
                (exceptionLog.ExceptionLocation != null && exceptionLog.ExceptionLocation.Contains(keyword)) ||
                (exceptionLog.RequestPath != null && exceptionLog.RequestPath.Contains(keyword)) ||
                (exceptionLog.RequestMethod != null && exceptionLog.RequestMethod.Contains(keyword)) ||
                (exceptionLog.ApplicationName != null && exceptionLog.ApplicationName.Contains(keyword)) ||
                (exceptionLog.EnvironmentName != null && exceptionLog.EnvironmentName.Contains(keyword)) ||
                (exceptionLog.ErrorCode != null && exceptionLog.ErrorCode.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, exceptionLog => exceptionLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.UserName != null && exceptionLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.SessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.RequestId))
        {
            var requestId = input.RequestId.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.RequestId == requestId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(input.ExceptionType))
        {
            var exceptionType = input.ExceptionType.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.ExceptionType == exceptionType);
        }

        if (!string.IsNullOrWhiteSpace(input.ExceptionSource))
        {
            var exceptionSource = input.ExceptionSource.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.ExceptionSource == exceptionSource);
        }

        if (!string.IsNullOrWhiteSpace(input.ExceptionLocation))
        {
            var exceptionLocation = input.ExceptionLocation.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.ExceptionLocation != null && exceptionLog.ExceptionLocation.Contains(exceptionLocation));
        }

        if (input.SeverityLevel.HasValue)
        {
            var severityLevel = input.SeverityLevel.Value;
            predicate = And(predicate, exceptionLog => exceptionLog.SeverityLevel == severityLevel);
        }

        if (!string.IsNullOrWhiteSpace(input.RequestPath))
        {
            var requestPath = input.RequestPath.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.RequestPath != null && exceptionLog.RequestPath.Contains(requestPath));
        }

        if (!string.IsNullOrWhiteSpace(input.RequestMethod))
        {
            var requestMethod = input.RequestMethod.Trim().ToUpperInvariant();
            predicate = And(predicate, exceptionLog => exceptionLog.RequestMethod == requestMethod);
        }

        if (input.StatusCode.HasValue)
        {
            var statusCode = input.StatusCode.Value;
            predicate = And(predicate, exceptionLog => exceptionLog.StatusCode == statusCode);
        }

        if (input.DeviceType.HasValue)
        {
            var deviceType = input.DeviceType.Value;
            predicate = And(predicate, exceptionLog => exceptionLog.DeviceType == deviceType);
        }

        if (!string.IsNullOrWhiteSpace(input.ApplicationName))
        {
            var applicationName = input.ApplicationName.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.ApplicationName == applicationName);
        }

        if (!string.IsNullOrWhiteSpace(input.ApplicationVersion))
        {
            var applicationVersion = input.ApplicationVersion.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.ApplicationVersion == applicationVersion);
        }

        if (!string.IsNullOrWhiteSpace(input.EnvironmentName))
        {
            var environmentName = input.EnvironmentName.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.EnvironmentName == environmentName);
        }

        if (input.IsHandled.HasValue)
        {
            var isHandled = input.IsHandled.Value;
            predicate = And(predicate, exceptionLog => exceptionLog.IsHandled == isHandled);
        }

        if (input.HandledBy.HasValue)
        {
            var handledBy = input.HandledBy.Value;
            predicate = And(predicate, exceptionLog => exceptionLog.HandledBy == handledBy);
        }

        if (!string.IsNullOrWhiteSpace(input.ErrorCode))
        {
            var errorCode = input.ErrorCode.Trim();
            predicate = And(predicate, exceptionLog => exceptionLog.ErrorCode == errorCode);
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
    private static void ValidatePageInput(ExceptionLogPageQueryDto input)
    {
        if (!input.ExceptionTimeStart.HasValue || !input.ExceptionTimeEnd.HasValue)
        {
            throw new ArgumentException("异常日志分表查询必须提供异常开始时间和异常结束时间。", nameof(input));
        }

        if (input.ExceptionTimeStart.Value > input.ExceptionTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.ExceptionTimeStart), "异常开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateOptionalId(input.HandledBy, nameof(input.HandledBy), "处理人主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.UserName, 50, nameof(input.UserName), "用户名长度不能超过 50。");
        ValidateMaxLength(input.SessionId, 100, nameof(input.SessionId), "会话标识长度不能超过 100。");
        ValidateMaxLength(input.RequestId, 100, nameof(input.RequestId), "请求标识长度不能超过 100。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");
        ValidateMaxLength(input.ExceptionType, 200, nameof(input.ExceptionType), "异常类型长度不能超过 200。");
        ValidateMaxLength(input.ExceptionSource, 200, nameof(input.ExceptionSource), "异常源长度不能超过 200。");
        ValidateMaxLength(input.ExceptionLocation, 300, nameof(input.ExceptionLocation), "异常发生位置长度不能超过 300。");
        ValidateMaxLength(input.RequestPath, 500, nameof(input.RequestPath), "请求路径长度不能超过 500。");
        ValidateMaxLength(input.RequestMethod, 10, nameof(input.RequestMethod), "请求方法长度不能超过 10。");
        ValidateMaxLength(input.ApplicationName, 100, nameof(input.ApplicationName), "应用程序名称长度不能超过 100。");
        ValidateMaxLength(input.ApplicationVersion, 50, nameof(input.ApplicationVersion), "应用程序版本长度不能超过 50。");
        ValidateMaxLength(input.EnvironmentName, 50, nameof(input.EnvironmentName), "环境名称长度不能超过 50。");
        ValidateMaxLength(input.ErrorCode, 50, nameof(input.ErrorCode), "错误代码长度不能超过 50。");
        ValidateStatusCode(input.StatusCode);

        if (input.SeverityLevel is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(input.SeverityLevel), "严重级别必须在 1 到 5 之间。");
        }

        if (input.DeviceType.HasValue && !Enum.IsDefined(input.DeviceType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.DeviceType), "设备类型无效。");
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
