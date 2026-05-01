#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogQueryService
// Guid:fb53d9de-1405-4923-a3d7-c1ca1791af1e
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
/// 登录日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "登录日志")]
public sealed class LoginLogQueryService(ILoginLogRepository loginLogRepository)
    : SaasApplicationService, ILoginLogQueryService
{
    /// <summary>
    /// 登录日志仓储
    /// </summary>
    private readonly ILoginLogRepository _loginLogRepository = loginLogRepository;

    /// <summary>
    /// 获取登录日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.LoginLog.Read)]
    public async Task<PageResultDtoBase<LoginLogListItemDto>> GetLoginLogPageAsync(LoginLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.LoginTimeStart!.Value;
        var endTime = input.LoginTimeEnd!.Value;
        var predicate = BuildLoginLogPredicate(input, beginTime, endTime);
        var loginLogPage = await _loginLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            loginLog => loginLog.LoginTime,
            false,
            cancellationToken);

        if (loginLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<LoginLogListItemDto>([], loginLogPage.Page);
        }

        var items = loginLogPage.Items
            .Select(LoginLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<LoginLogListItemDto>(items, loginLogPage.Page);
    }

    /// <summary>
    /// 获取登录日志详情
    /// </summary>
    /// <param name="id">登录日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.LoginLog.Read)]
    public async Task<LoginLogDetailDto?> GetLoginLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "登录日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var loginLog = await _loginLogRepository.GetByIdAsync(id, cancellationToken);
        return loginLog is null ? null : LoginLogApplicationMapper.ToDetailDto(loginLog);
    }

    /// <summary>
    /// 构建登录日志查询表达式
    /// </summary>
    private static Expression<Func<SysLoginLog, bool>> BuildLoginLogPredicate(LoginLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysLoginLog, bool>> predicate = loginLog => loginLog.LoginTime >= beginTime && loginLog.LoginTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, loginLog =>
                (loginLog.UserName != null && loginLog.UserName.Contains(keyword)) ||
                (loginLog.SessionId != null && loginLog.SessionId.Contains(keyword)) ||
                (loginLog.TraceId != null && loginLog.TraceId.Contains(keyword)));
        }

        if (input.UserId.HasValue)
        {
            var userId = input.UserId.Value;
            predicate = And(predicate, loginLog => loginLog.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userName = input.UserName.Trim();
            predicate = And(predicate, loginLog => loginLog.UserName != null && loginLog.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(input.SessionId))
        {
            var sessionId = input.SessionId.Trim();
            predicate = And(predicate, loginLog => loginLog.SessionId == sessionId);
        }

        if (!string.IsNullOrWhiteSpace(input.TraceId))
        {
            var traceId = input.TraceId.Trim();
            predicate = And(predicate, loginLog => loginLog.TraceId == traceId);
        }

        if (input.LoginResult.HasValue)
        {
            var loginResult = input.LoginResult.Value;
            predicate = And(predicate, loginLog => loginLog.LoginResult == loginResult);
        }

        if (input.IsRiskLogin.HasValue)
        {
            var isRiskLogin = input.IsRiskLogin.Value;
            predicate = And(predicate, loginLog => loginLog.IsRiskLogin == isRiskLogin);
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
    private static void ValidatePageInput(LoginLogPageQueryDto input)
    {
        if (!input.LoginTimeStart.HasValue || !input.LoginTimeEnd.HasValue)
        {
            throw new ArgumentException("登录日志分表查询必须提供登录开始时间和登录结束时间。", nameof(input));
        }

        if (input.LoginTimeStart.Value > input.LoginTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.LoginTimeStart), "登录开始时间不能晚于结束时间。");
        }

        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.UserName, 50, nameof(input.UserName), "用户名长度不能超过 50。");
        ValidateMaxLength(input.SessionId, 100, nameof(input.SessionId), "会话标识长度不能超过 100。");
        ValidateMaxLength(input.TraceId, 64, nameof(input.TraceId), "链路追踪 ID 长度不能超过 64。");

        if (input.LoginResult.HasValue && !Enum.IsDefined(input.LoginResult.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.LoginResult), "登录结果无效。");
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
