#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ApiLogQueryService
// Guid:637404a9-bf10-4580-9d50-ef87bd85ec74
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
/// API 日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "API日志")]
public sealed class ApiLogQueryService(IApiLogRepository apiLogRepository)
    : SaasApplicationService, IApiLogQueryService
{
    /// <summary>
    /// API 日志仓储
    /// </summary>
    private readonly IApiLogRepository _apiLogRepository = apiLogRepository;

    /// <summary>
    /// 获取 API 日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API 日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.ApiLog.Read)]
    public async Task<PageResultDtoBase<ApiLogListItemDto>> GetApiLogPageAsync(ApiLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.RequestTimeStart!.Value;
        var endTime = input.RequestTimeEnd!.Value;
        var predicate = BuildApiLogPredicate(input, beginTime, endTime);
        var apiLogPage = await _apiLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            apiLog => apiLog.RequestTime,
            false,
            cancellationToken);

        if (apiLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<ApiLogListItemDto>([], apiLogPage.Page);
        }

        var items = apiLogPage.Items
            .Select(ApiLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<ApiLogListItemDto>(items, apiLogPage.Page);
    }

    /// <summary>
    /// 获取 API 日志详情
    /// </summary>
    /// <param name="id">API 日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API 日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.ApiLog.Read)]
    public async Task<ApiLogDetailDto?> GetApiLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "API 日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var apiLog = await _apiLogRepository.GetByIdAsync(id, cancellationToken);
        return apiLog is null ? null : ApiLogApplicationMapper.ToDetailDto(apiLog);
    }

    /// <summary>
    /// 构建 API 日志查询表达式
    /// </summary>
    private static Expression<Func<SysApiLog, bool>> BuildApiLogPredicate(ApiLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysApiLog, bool>> predicate = apiLog => apiLog.RequestTime >= beginTime && apiLog.RequestTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, apiLog =>
                (apiLog.UserName != null && apiLog.UserName.Contains(keyword)) ||
                (apiLog.SessionId != null && apiLog.SessionId.Contains(keyword)) ||
                (apiLog.RequestId != null && apiLog.RequestId.Contains(keyword)) ||
                (apiLog.TraceId != null && apiLog.TraceId.Contains(keyword)) ||
                (apiLog.ClientId != null && apiLog.ClientId.Contains(keyword)) ||
                (apiLog.AppId != null && apiLog.AppId.Contains(keyword)) ||
                apiLog.ApiPath.Contains(keyword) ||
                (apiLog.ApiName != null && apiLog.ApiName.Contains(keyword)) ||
                apiLog.Method.Contains(keyword) ||
                (apiLog.ApiVersion != null && apiLog.ApiVersion.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, apiLog => apiLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, apiLog => apiLog.UserName != null && apiLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, apiLog => apiLog.SessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.RequestId))
        {
            var requestId = input.RequestId.Trim();
            predicate = And(predicate, apiLog => apiLog.RequestId == requestId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, apiLog => apiLog.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(input.ClientId))
        {
            var clientId = input.ClientId.Trim();
            predicate = And(predicate, apiLog => apiLog.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(input.AppId))
        {
            var appId = input.AppId.Trim();
            predicate = And(predicate, apiLog => apiLog.AppId == appId);
        }

        if (!string.IsNullOrWhiteSpace(input.ApiPath))
        {
            var apiPath = input.ApiPath.Trim();
            predicate = And(predicate, apiLog => apiLog.ApiPath.Contains(apiPath));
        }

        if (!string.IsNullOrWhiteSpace(input.Method))
        {
            var method = input.Method.Trim().ToUpperInvariant();
            predicate = And(predicate, apiLog => apiLog.Method == method);
        }

        if (input.StatusCode.HasValue)
        {
            var statusCode = input.StatusCode.Value;
            predicate = And(predicate, apiLog => apiLog.StatusCode == statusCode);
        }

        if (input.IsSuccess.HasValue)
        {
            var isSuccess = input.IsSuccess.Value;
            predicate = And(predicate, apiLog => apiLog.IsSuccess == isSuccess);
        }

        if (input.IsSignatureValid.HasValue)
        {
            var isSignatureValid = input.IsSignatureValid.Value;
            predicate = And(predicate, apiLog => apiLog.IsSignatureValid == isSignatureValid);
        }

        if (input.SignatureType.HasValue)
        {
            var signatureType = input.SignatureType.Value;
            predicate = And(predicate, apiLog => apiLog.SignatureType == signatureType);
        }

        if (!string.IsNullOrWhiteSpace(input.ApiVersion))
        {
            var apiVersion = input.ApiVersion.Trim();
            predicate = And(predicate, apiLog => apiLog.ApiVersion == apiVersion);
        }

        if (input.MinExecutionTime.HasValue)
        {
            var minExecutionTime = input.MinExecutionTime.Value;
            predicate = And(predicate, apiLog => apiLog.ExecutionTime >= minExecutionTime);
        }

        if (input.MaxExecutionTime.HasValue)
        {
            var maxExecutionTime = input.MaxExecutionTime.Value;
            predicate = And(predicate, apiLog => apiLog.ExecutionTime <= maxExecutionTime);
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
    private static void ValidatePageInput(ApiLogPageQueryDto input)
    {
        if (!input.RequestTimeStart.HasValue || !input.RequestTimeEnd.HasValue)
        {
            throw new ArgumentException("API 日志分表查询必须提供请求开始时间和请求结束时间。", nameof(input));
        }

        if (input.RequestTimeStart.Value > input.RequestTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.RequestTimeStart), "请求开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.UserName, 50, nameof(input.UserName), "用户名长度不能超过 50。");
        ValidateMaxLength(input.SessionId, 100, nameof(input.SessionId), "会话标识长度不能超过 100。");
        ValidateMaxLength(input.RequestId, 100, nameof(input.RequestId), "请求标识长度不能超过 100。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");
        ValidateMaxLength(input.ClientId, 100, nameof(input.ClientId), "客户端标识长度不能超过 100。");
        ValidateMaxLength(input.AppId, 100, nameof(input.AppId), "应用标识长度不能超过 100。");
        ValidateMaxLength(input.ApiPath, 500, nameof(input.ApiPath), "API 路径长度不能超过 500。");
        ValidateMaxLength(input.Method, 10, nameof(input.Method), "请求方法长度不能超过 10。");
        ValidateMaxLength(input.ApiVersion, 20, nameof(input.ApiVersion), "API 版本长度不能超过 20。");
        ValidateStatusCode(input.StatusCode);
        ValidateExecutionTime(input.MinExecutionTime, nameof(input.MinExecutionTime), "最小执行耗时不能小于 0。");
        ValidateExecutionTime(input.MaxExecutionTime, nameof(input.MaxExecutionTime), "最大执行耗时不能小于 0。");

        if (input.SignatureType.HasValue && !Enum.IsDefined(input.SignatureType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.SignatureType), "签名类型无效。");
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
