#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DiffLogQueryService
// Guid:7d29f61d-9e56-40d9-9690-79619c4f52c1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 差异日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "差异日志")]
public sealed class DiffLogQueryService
    : SaasApplicationService, IDiffLogQueryService
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DiffLogQueryService(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 获取差异日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>差异日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.DiffLog.Read)]
    public async Task<PageResultDtoBase<DiffLogListItemDto>> GetDiffLogPageAsync(DiffLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var predicate = BuildDiffLogPredicate(input);
        RefAsync<int> totalCount = 0;
        var entities = await DbClient.Queryable<SysDiffLog>()
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.AuditTime)
            .ToPageListAsync(input.Page.PageIndex, input.Page.PageSize, totalCount, cancellationToken);

        var page = new PageResultMetadata(input.Page.PageIndex, input.Page.PageSize, totalCount);
        if (entities.Count == 0)
        {
            return new PageResultDtoBase<DiffLogListItemDto>([], page);
        }

        var items = entities
            .Select(DiffLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<DiffLogListItemDto>(items, page);
    }

    /// <summary>
    /// 获取差异日志详情
    /// </summary>
    /// <param name="id">差异日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>差异日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.DiffLog.Read)]
    public async Task<DiffLogDetailDto?> GetDiffLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "差异日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var diffLog = await DbClient.Queryable<SysDiffLog>()
            .Where(x => x.BasicId == id)
            .SplitTable()
            .FirstAsync(cancellationToken);
        return diffLog is null ? null : DiffLogApplicationMapper.ToDetailDto(diffLog);
    }

    /// <summary>
    /// 构建差异日志查询表达式
    /// </summary>
    private static Expression<Func<SysDiffLog, bool>> BuildDiffLogPredicate(DiffLogPageQueryDto input)
    {
        Expression<Func<SysDiffLog, bool>> predicate = diffLog => true;

        if (input.AuditTimeStart.HasValue)
        {
            var beginTime = input.AuditTimeStart.Value;
            predicate = And(predicate, diffLog => diffLog.AuditTime >= beginTime);
        }

        if (input.AuditTimeEnd.HasValue)
        {
            var endTime = input.AuditTimeEnd.Value;
            predicate = And(predicate, diffLog => diffLog.AuditTime <= endTime);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, diffLog =>
                (diffLog.UserName != null && diffLog.UserName.Contains(keyword)) ||
                (diffLog.SessionId != null && diffLog.SessionId.Contains(keyword)) ||
                (diffLog.RequestId != null && diffLog.RequestId.Contains(keyword)) ||
                (diffLog.TraceId != null && diffLog.TraceId.Contains(keyword)) ||
                diffLog.AuditType.Contains(keyword) ||
                (diffLog.EntityType != null && diffLog.EntityType.Contains(keyword)) ||
                (diffLog.EntityId != null && diffLog.EntityId.Contains(keyword)) ||
                (diffLog.EntityName != null && diffLog.EntityName.Contains(keyword)) ||
                (diffLog.TableName != null && diffLog.TableName.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, diffLog => diffLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, diffLog => diffLog.UserName != null && diffLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, diffLog => diffLog.SessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.RequestId))
        {
            var requestId = input.RequestId.Trim();
            predicate = And(predicate, diffLog => diffLog.RequestId == requestId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, diffLog => diffLog.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(input.AuditType))
        {
            var diffLogType = input.AuditType.Trim();
            predicate = And(predicate, diffLog => diffLog.AuditType == diffLogType);
        }

        if (input.OperationType.HasValue)
        {
            var operationType = input.OperationType.Value;
            predicate = And(predicate, diffLog => diffLog.OperationType == operationType);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityType))
        {
            var entityType = input.EntityType.Trim();
            predicate = And(predicate, diffLog => diffLog.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityId))
        {
            var entityId = input.EntityId.Trim();
            predicate = And(predicate, diffLog => diffLog.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityName))
        {
            var entityName = input.EntityName.Trim();
            predicate = And(predicate, diffLog => diffLog.EntityName != null && diffLog.EntityName.Contains(entityName));
        }

        if (!string.IsNullOrWhiteSpace(input.TableName))
        {
            var tableName = input.TableName.Trim();
            predicate = And(predicate, diffLog => diffLog.TableName == tableName);
        }

        if (input.IsSuccess.HasValue)
        {
            var isSuccess = input.IsSuccess.Value;
            predicate = And(predicate, diffLog => diffLog.IsSuccess == isSuccess);
        }

        if (input.RiskLevel.HasValue)
        {
            var riskLevel = input.RiskLevel.Value;
            predicate = And(predicate, diffLog => diffLog.RiskLevel == riskLevel);
        }

        if (input.MinExecutionTime.HasValue)
        {
            var minExecutionTime = input.MinExecutionTime.Value;
            predicate = And(predicate, diffLog => diffLog.ExecutionTime >= minExecutionTime);
        }

        if (input.MaxExecutionTime.HasValue)
        {
            var maxExecutionTime = input.MaxExecutionTime.Value;
            predicate = And(predicate, diffLog => diffLog.ExecutionTime <= maxExecutionTime);
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
    private static void ValidatePageInput(DiffLogPageQueryDto input)
    {
        if (input.AuditTimeStart.HasValue && input.AuditTimeEnd.HasValue &&
            input.AuditTimeStart.Value > input.AuditTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.AuditTimeStart), "审计开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.UserName, 50, nameof(input.UserName), "用户名长度不能超过 50。");
        ValidateMaxLength(input.SessionId, 100, nameof(input.SessionId), "会话标识长度不能超过 100。");
        ValidateMaxLength(input.RequestId, 100, nameof(input.RequestId), "请求标识长度不能超过 100。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");
        ValidateMaxLength(input.AuditType, 50, nameof(input.AuditType), "审计类型长度不能超过 50。");
        ValidateMaxLength(input.EntityType, 100, nameof(input.EntityType), "实体类型长度不能超过 100。");
        ValidateMaxLength(input.EntityId, 100, nameof(input.EntityId), "实体 ID 长度不能超过 100。");
        ValidateMaxLength(input.EntityName, 200, nameof(input.EntityName), "实体名称长度不能超过 200。");
        ValidateMaxLength(input.TableName, 100, nameof(input.TableName), "表名称长度不能超过 100。");
        ValidateExecutionTime(input.MinExecutionTime, nameof(input.MinExecutionTime), "最小执行耗时不能小于 0。");
        ValidateExecutionTime(input.MaxExecutionTime, nameof(input.MaxExecutionTime), "最大执行耗时不能小于 0。");

        if (input.OperationType.HasValue && !Enum.IsDefined(input.OperationType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.OperationType), "操作类型无效。");
        }

        if (input.RiskLevel.HasValue && !Enum.IsDefined(input.RiskLevel.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.RiskLevel), "风险等级无效。");
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
