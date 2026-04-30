#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionQueryService
// Guid:bf3029aa-6e13-4085-a518-218a9c53d428
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户会话查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户会话")]
public sealed class UserSessionQueryService(
    IUserSessionRepository userSessionRepository,
    IUserRepository userRepository)
    : SaasApplicationService, IUserSessionQueryService
{
    /// <summary>
    /// 用户会话仓储
    /// </summary>
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取用户会话分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Read)]
    public async Task<PageResultDtoBase<UserSessionListItemDto>> GetUserSessionPageAsync(UserSessionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildUserSessionPageRequest(input);
        var sessionPage = await _userSessionRepository.GetPagedAsync(request, cancellationToken);
        if (sessionPage.Items.Count == 0)
        {
            return new PageResultDtoBase<UserSessionListItemDto>([], sessionPage.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var userMap = await BuildUserMapAsync(sessionPage.Items.Select(session => session.UserId), cancellationToken);
        var items = sessionPage.Items
            .Select(session => UserSessionApplicationMapper.ToListItemDto(
                session,
                userMap.GetValueOrDefault(session.UserId),
                now))
            .ToList();

        return new PageResultDtoBase<UserSessionListItemDto>(items, sessionPage.Page);
    }

    /// <summary>
    /// 获取用户会话详情
    /// </summary>
    /// <param name="id">会话主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Read)]
    public async Task<UserSessionDetailDto?> GetUserSessionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "会话主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var session = await _userSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        return UserSessionApplicationMapper.ToDetailDto(session, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建用户会话分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户会话分页请求</returns>
    private static BasicAppPRDto BuildUserSessionPageRequest(UserSessionPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysUserSession.UserSessionId),
                nameof(SysUserSession.DeviceName),
                nameof(SysUserSession.OperatingSystem),
                nameof(SysUserSession.Browser),
                nameof(SysUserSession.Remark));
        }

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.UserId), input.UserId.Value);
        }

        if (input.DeviceType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.DeviceType), input.DeviceType.Value);
        }

        if (input.IsOnline.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.IsOnline), input.IsOnline.Value);
        }

        if (input.IsRevoked.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.IsRevoked), input.IsRevoked.Value);
        }

        if (input.LoginTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.LoginTime), input.LoginTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.LoginTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.LoginTime), input.LoginTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        if (input.LastActivityTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.LastActivityTime), input.LastActivityTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.LastActivityTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSession.LastActivityTime), input.LastActivityTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysUserSession.IsOnline), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysUserSession.LastActivityTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysUserSession.LoginTime), SortDirection.Descending, 2);
        return request;
    }

    /// <summary>
    /// 构建用户映射
    /// </summary>
    /// <param name="userIds">用户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户映射</returns>
    private async Task<IReadOnlyDictionary<long, SysUser>> BuildUserMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUser>();
        }

        var users = await _userRepository.GetByIdsAsync(ids, cancellationToken);
        return users.ToDictionary(user => user.BasicId);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(UserSessionPageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        if (input.DeviceType.HasValue)
        {
            ValidateEnum(input.DeviceType.Value, nameof(input.DeviceType));
        }

        ValidateRange(input.LoginTimeStart, input.LoginTimeEnd, nameof(input.LoginTimeStart), "登录开始时间不能晚于结束时间。");
        ValidateRange(input.LastActivityTimeStart, input.LastActivityTimeEnd, nameof(input.LastActivityTimeStart), "最后活动开始时间不能晚于结束时间。");
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
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验时间范围
    /// </summary>
    private static void ValidateRange(DateTimeOffset? start, DateTimeOffset? end, string paramName, string message)
    {
        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }
}
