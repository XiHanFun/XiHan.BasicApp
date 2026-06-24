#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogQueryService
// Guid:fb80d0df-2ca0-4d9c-a3fa-3d80038e6465
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 操作日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "操作日志")]
public sealed class OperationLogQueryService
    : SaasApplicationService, IOperationLogQueryService
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 字段级安全（排序门控：剔除不可读/已脱敏字段）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationLogQueryService(
        ISqlSugarClientResolver clientResolver,
        IFieldSecurityService fieldSecurityService)
    {
        _clientResolver = clientResolver;
        _fieldSecurity = fieldSecurityService;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 获取操作日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.OperationLog.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<OperationLogListItemDto>> GetOperationLogPageAsync(OperationLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var predicate = BuildOperationLogPredicate(input);

        // 排序/过滤：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认按操作时间倒序
        await _fieldSecurity.GuardSortsAsync(input.Conditions, "SysOperationLog", cancellationToken);
        await _fieldSecurity.GuardFiltersAsync(input.Conditions, "SysOperationLog", cancellationToken);
        var query = DbClient.Queryable<SysOperationLog>()
            .Where(predicate)
            .SplitTable();
        query = query.ApplyFilters(input.Conditions.Filters);
        query = input.Conditions.Sorts.Count > 0
            ? query.ApplySorts(input.Conditions.Sorts)
            : query.OrderByDescending(x => x.OperationTime);

        RefAsync<int> totalCount = 0;
        var entities = await query
            .ToPageListAsync(input.Page.PageIndex, input.Page.PageSize, totalCount, cancellationToken);

        var page = new PageResultMetadata(input.Page.PageIndex, input.Page.PageSize, totalCount);
        if (entities.Count == 0)
        {
            return new PageResultDtoBase<OperationLogListItemDto>([], page);
        }

        var items = entities
            .Select(OperationLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<OperationLogListItemDto>(items, page);
    }

    /// <summary>
    /// 获取操作日志详情
    /// </summary>
    /// <param name="id">操作日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.OperationLog.Read)]
    public async Task<OperationLogDetailDto?> GetOperationLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "操作日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var operationLog = await DbClient.Queryable<SysOperationLog>()
            .Where(x => x.BasicId == id)
            .SplitTable()
            .FirstAsync(cancellationToken);
        return operationLog is null ? null : OperationLogApplicationMapper.ToDetailDto(operationLog);
    }

    /// <summary>
    /// 构建操作日志查询表达式
    /// </summary>
    private static Expression<Func<SysOperationLog, bool>> BuildOperationLogPredicate(OperationLogPageQueryDto input)
    {
        Expression<Func<SysOperationLog, bool>> predicate = operationLog => true;

        if (input.OperationTimeStart.HasValue)
        {
            var beginTime = input.OperationTimeStart.Value;
            predicate = And(predicate, operationLog => operationLog.OperationTime >= beginTime);
        }

        if (input.OperationTimeEnd.HasValue)
        {
            var endTime = input.OperationTimeEnd.Value;
            predicate = And(predicate, operationLog => operationLog.OperationTime <= endTime);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, operationLog =>
                (operationLog.UserName != null && operationLog.UserName.Contains(keyword)) ||
                (operationLog.UserSessionId != null && operationLog.UserSessionId.Contains(keyword)) ||
                (operationLog.TraceId != null && operationLog.TraceId.Contains(keyword)) ||
                (operationLog.Module != null && operationLog.Module.Contains(keyword)) ||
                (operationLog.Function != null && operationLog.Function.Contains(keyword)) ||
                (operationLog.Title != null && operationLog.Title.Contains(keyword)) ||
                (operationLog.Method != null && operationLog.Method.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, operationLog => operationLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, operationLog => operationLog.UserName != null && operationLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, operationLog => operationLog.UserSessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, operationLog => operationLog.TraceId == traceId);
        }

        if (input.OperationType.HasValue)
        {
            var operationType = input.OperationType.Value;
            predicate = And(predicate, operationLog => operationLog.OperationType == operationType);
        }

        if (!string.IsNullOrWhiteSpace(input.Module))
        {
            var module = input.Module.Trim();
            predicate = And(predicate, operationLog => operationLog.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(input.Function))
        {
            var function = input.Function.Trim();
            predicate = And(predicate, operationLog => operationLog.Function == function);
        }

        if (!string.IsNullOrWhiteSpace(input.Title))
        {
            var title = input.Title.Trim();
            predicate = And(predicate, operationLog => operationLog.Title != null && operationLog.Title.Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(input.Method))
        {
            var method = input.Method.Trim().ToUpperInvariant();
            predicate = And(predicate, operationLog => operationLog.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(input.OperationIp))
        {
            var operationIp = input.OperationIp.Trim();
            predicate = And(predicate, operationLog => operationLog.OperationIp != null && operationLog.OperationIp.Contains(operationIp));
        }

        if (input.Result.HasValue)
        {
            var result = input.Result.Value;
            predicate = And(predicate, operationLog => operationLog.Result == result);
        }

        if (input.MinExecutionTime.HasValue)
        {
            var minExecutionTime = input.MinExecutionTime.Value;
            predicate = And(predicate, operationLog => operationLog.ExecutionTime >= minExecutionTime);
        }

        if (input.MaxExecutionTime.HasValue)
        {
            var maxExecutionTime = input.MaxExecutionTime.Value;
            predicate = And(predicate, operationLog => operationLog.ExecutionTime <= maxExecutionTime);
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
    private static void ValidatePageInput(OperationLogPageQueryDto input)
    {
        if (input.OperationTimeStart.HasValue && input.OperationTimeEnd.HasValue &&
            input.OperationTimeStart.Value > input.OperationTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.OperationTimeStart), "操作开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.UserName, 50, nameof(input.UserName), "用户名长度不能超过 50。");
        ValidateMaxLength(input.SessionId, 100, nameof(input.SessionId), "会话标识长度不能超过 100。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");
        ValidateMaxLength(input.Module, 50, nameof(input.Module), "操作模块长度不能超过 50。");
        ValidateMaxLength(input.Function, 50, nameof(input.Function), "操作功能长度不能超过 50。");
        ValidateMaxLength(input.Title, 200, nameof(input.Title), "操作标题长度不能超过 200。");
        ValidateMaxLength(input.Method, 10, nameof(input.Method), "请求方法长度不能超过 10。");
        ValidateMaxLength(input.OperationIp, 64, nameof(input.OperationIp), "操作 IP 长度不能超过 64。");
        ValidateExecutionTime(input.MinExecutionTime, nameof(input.MinExecutionTime), "最小执行耗时不能小于 0。");
        ValidateExecutionTime(input.MaxExecutionTime, nameof(input.MaxExecutionTime), "最大执行耗时不能小于 0。");

        if (input.OperationType.HasValue && !Enum.IsDefined(input.OperationType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.OperationType), "操作类型无效。");
        }

        if (input.Result.HasValue && !Enum.IsDefined(input.Result.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.Result), "操作执行结果无效。");
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
