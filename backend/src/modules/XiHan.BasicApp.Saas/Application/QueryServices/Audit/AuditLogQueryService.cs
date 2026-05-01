#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditLogQueryService
// Guid:7d29f61d-9e56-40d9-9690-79619c4f52c1
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
/// 审计日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "审计日志")]
public sealed class AuditLogQueryService(IAuditLogRepository auditLogRepository)
    : SaasApplicationService, IAuditLogQueryService
{
    /// <summary>
    /// 审计日志仓储
    /// </summary>
    private readonly IAuditLogRepository _auditLogRepository = auditLogRepository;

    /// <summary>
    /// 获取审计日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.AuditLog.Read)]
    public async Task<PageResultDtoBase<AuditLogListItemDto>> GetAuditLogPageAsync(AuditLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.AuditTimeStart!.Value;
        var endTime = input.AuditTimeEnd!.Value;
        var predicate = BuildAuditLogPredicate(input, beginTime, endTime);
        var auditLogPage = await _auditLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            auditLog => auditLog.AuditTime,
            false,
            cancellationToken);

        if (auditLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<AuditLogListItemDto>([], auditLogPage.Page);
        }

        var items = auditLogPage.Items
            .Select(AuditLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<AuditLogListItemDto>(items, auditLogPage.Page);
    }

    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    /// <param name="id">审计日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.AuditLog.Read)]
    public async Task<AuditLogDetailDto?> GetAuditLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "审计日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var auditLog = await _auditLogRepository.GetByIdAsync(id, cancellationToken);
        return auditLog is null ? null : AuditLogApplicationMapper.ToDetailDto(auditLog);
    }

    /// <summary>
    /// 构建审计日志查询表达式
    /// </summary>
    private static Expression<Func<SysAuditLog, bool>> BuildAuditLogPredicate(AuditLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysAuditLog, bool>> predicate = auditLog => auditLog.AuditTime >= beginTime && auditLog.AuditTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, auditLog =>
                (auditLog.UserName != null && auditLog.UserName.Contains(keyword)) ||
                (auditLog.SessionId != null && auditLog.SessionId.Contains(keyword)) ||
                (auditLog.RequestId != null && auditLog.RequestId.Contains(keyword)) ||
                (auditLog.TraceId != null && auditLog.TraceId.Contains(keyword)) ||
                auditLog.AuditType.Contains(keyword) ||
                (auditLog.EntityType != null && auditLog.EntityType.Contains(keyword)) ||
                (auditLog.EntityId != null && auditLog.EntityId.Contains(keyword)) ||
                (auditLog.EntityName != null && auditLog.EntityName.Contains(keyword)) ||
                (auditLog.TableName != null && auditLog.TableName.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, auditLog => auditLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, auditLog => auditLog.UserName != null && auditLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, auditLog => auditLog.SessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.RequestId))
        {
            var requestId = input.RequestId.Trim();
            predicate = And(predicate, auditLog => auditLog.RequestId == requestId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, auditLog => auditLog.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(input.AuditType))
        {
            var auditType = input.AuditType.Trim();
            predicate = And(predicate, auditLog => auditLog.AuditType == auditType);
        }

        if (input.OperationType.HasValue)
        {
            var operationType = input.OperationType.Value;
            predicate = And(predicate, auditLog => auditLog.OperationType == operationType);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityType))
        {
            var entityType = input.EntityType.Trim();
            predicate = And(predicate, auditLog => auditLog.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityId))
        {
            var entityId = input.EntityId.Trim();
            predicate = And(predicate, auditLog => auditLog.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityName))
        {
            var entityName = input.EntityName.Trim();
            predicate = And(predicate, auditLog => auditLog.EntityName != null && auditLog.EntityName.Contains(entityName));
        }

        if (!string.IsNullOrWhiteSpace(input.TableName))
        {
            var tableName = input.TableName.Trim();
            predicate = And(predicate, auditLog => auditLog.TableName == tableName);
        }

        if (input.IsSuccess.HasValue)
        {
            var isSuccess = input.IsSuccess.Value;
            predicate = And(predicate, auditLog => auditLog.IsSuccess == isSuccess);
        }

        if (input.RiskLevel.HasValue)
        {
            var riskLevel = input.RiskLevel.Value;
            predicate = And(predicate, auditLog => auditLog.RiskLevel == riskLevel);
        }

        if (input.MinExecutionTime.HasValue)
        {
            var minExecutionTime = input.MinExecutionTime.Value;
            predicate = And(predicate, auditLog => auditLog.ExecutionTime >= minExecutionTime);
        }

        if (input.MaxExecutionTime.HasValue)
        {
            var maxExecutionTime = input.MaxExecutionTime.Value;
            predicate = And(predicate, auditLog => auditLog.ExecutionTime <= maxExecutionTime);
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
    private static void ValidatePageInput(AuditLogPageQueryDto input)
    {
        if (!input.AuditTimeStart.HasValue || !input.AuditTimeEnd.HasValue)
        {
            throw new ArgumentException("审计日志分表查询必须提供审计开始时间和审计结束时间。", nameof(input));
        }

        if (input.AuditTimeStart.Value > input.AuditTimeEnd.Value)
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
