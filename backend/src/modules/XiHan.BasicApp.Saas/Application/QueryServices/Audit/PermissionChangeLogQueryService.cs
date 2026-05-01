#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionChangeLogQueryService
// Guid:a0f06c0c-5847-4757-9bd1-0478bf0ac65d
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
/// 权限变更日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限变更日志")]
public sealed class PermissionChangeLogQueryService(IPermissionChangeLogRepository permissionChangeLogRepository)
    : SaasApplicationService, IPermissionChangeLogQueryService
{
    /// <summary>
    /// 权限变更日志仓储
    /// </summary>
    private readonly IPermissionChangeLogRepository _permissionChangeLogRepository = permissionChangeLogRepository;

    /// <summary>
    /// 获取权限变更日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限变更日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionChangeLog.Read)]
    public async Task<PageResultDtoBase<PermissionChangeLogListItemDto>> GetPermissionChangeLogPageAsync(PermissionChangeLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.ChangeTimeStart!.Value;
        var endTime = input.ChangeTimeEnd!.Value;
        var predicate = BuildPermissionChangeLogPredicate(input, beginTime, endTime);
        var permissionChangeLogPage = await _permissionChangeLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            permissionChangeLog => permissionChangeLog.ChangeTime,
            false,
            cancellationToken);

        if (permissionChangeLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<PermissionChangeLogListItemDto>([], permissionChangeLogPage.Page);
        }

        var items = permissionChangeLogPage.Items
            .Select(PermissionChangeLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<PermissionChangeLogListItemDto>(items, permissionChangeLogPage.Page);
    }

    /// <summary>
    /// 获取权限变更日志详情
    /// </summary>
    /// <param name="id">权限变更日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限变更日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionChangeLog.Read)]
    public async Task<PermissionChangeLogDetailDto?> GetPermissionChangeLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限变更日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var permissionChangeLog = await _permissionChangeLogRepository.GetByIdAsync(id, cancellationToken);
        return permissionChangeLog is null ? null : PermissionChangeLogApplicationMapper.ToDetailDto(permissionChangeLog);
    }

    /// <summary>
    /// 构建权限变更日志查询表达式
    /// </summary>
    private static Expression<Func<SysPermissionChangeLog, bool>> BuildPermissionChangeLogPredicate(PermissionChangeLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysPermissionChangeLog, bool>> predicate = permissionChangeLog => permissionChangeLog.ChangeTime >= beginTime && permissionChangeLog.ChangeTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.TraceId != null && permissionChangeLog.TraceId.Contains(keyword));
        }

        if (input.OperatorUserId.HasValue)
        {
            var operatorUserId = input.OperatorUserId.Value;
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.OperatorUserId == operatorUserId);
        }

        if (input.TargetUserId.HasValue)
        {
            var targetUserId = input.TargetUserId.Value;
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.TargetUserId == targetUserId);
        }

        if (input.TargetRoleId.HasValue)
        {
            var targetRoleId = input.TargetRoleId.Value;
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.TargetRoleId == targetRoleId);
        }

        if (input.PermissionId.HasValue)
        {
            var permissionId = input.PermissionId.Value;
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.PermissionId == permissionId);
        }

        if (input.ChangeType.HasValue)
        {
            var changeType = input.ChangeType.Value;
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.ChangeType == changeType);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, permissionChangeLog => permissionChangeLog.TraceId == traceId);
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
    private static void ValidatePageInput(PermissionChangeLogPageQueryDto input)
    {
        if (!input.ChangeTimeStart.HasValue || !input.ChangeTimeEnd.HasValue)
        {
            throw new ArgumentException("权限变更日志分表查询必须提供变更开始时间和变更结束时间。", nameof(input));
        }

        if (input.ChangeTimeStart.Value > input.ChangeTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.ChangeTimeStart), "变更开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.OperatorUserId, nameof(input.OperatorUserId), "操作人主键必须大于 0。");
        ValidateOptionalId(input.TargetUserId, nameof(input.TargetUserId), "目标用户主键必须大于 0。");
        ValidateOptionalId(input.TargetRoleId, nameof(input.TargetRoleId), "目标角色主键必须大于 0。");
        ValidateOptionalId(input.PermissionId, nameof(input.PermissionId), "权限主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");

        if (input.ChangeType.HasValue && !Enum.IsDefined(input.ChangeType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.ChangeType), "变更类型无效。");
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
