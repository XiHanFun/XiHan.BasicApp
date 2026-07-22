// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
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
/// OAuth Token 查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "OAuthToken")]
public sealed class OAuthTokenQueryService
    : SaasApplicationService, IOAuthTokenQueryService
{
    /// <summary>
    /// OAuth Token 仓储
    /// </summary>
    private readonly IOAuthTokenRepository _oauthTokenRepository;

    /// <summary>
    /// OAuth 应用仓储
    /// </summary>
    private readonly IOAuthAppRepository _oauthAppRepository;

    /// <summary>
    /// 用户会话仓储
    /// </summary>
    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthTokenQueryService(
        IOAuthTokenRepository oauthTokenRepository,
        IOAuthAppRepository oauthAppRepository,
        IUserSessionRepository userSessionRepository,
        IUserRepository userRepository)
    {
        _oauthTokenRepository = oauthTokenRepository;
        _oauthAppRepository = oauthAppRepository;
        _userSessionRepository = userSessionRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取 OAuth Token 分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth Token 分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.OAuthToken.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<OAuthTokenListItemDto>> GetOAuthTokenPageAsync(OAuthTokenPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildOAuthTokenPageRequest(input, DateTimeOffset.UtcNow);
        var tokenPage = await _oauthTokenRepository.GetPagedAsync(request, cancellationToken);
        if (tokenPage.Items.Count == 0)
        {
            return new PageResultDtoBase<OAuthTokenListItemDto>([], tokenPage.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var appMap = await BuildAppMapAsync(tokenPage.Items.Select(token => token.ClientId), cancellationToken);
        var sessionMap = await BuildSessionMapAsync(tokenPage.Items.Select(token => token.SessionId), cancellationToken);
        var userIds = tokenPage.Items
            .Select(token => token.UserId)
            .Concat(sessionMap.Values.Select(session => (long?)session.UserId));
        var userMap = await BuildUserMapAsync(userIds, cancellationToken);
        var items = tokenPage.Items
            .Select(token =>
            {
                var session = token.SessionId.HasValue
                    ? sessionMap.GetValueOrDefault(token.SessionId.Value)
                    : null;
                var userId = token.UserId ?? session?.UserId;
                var user = userId.HasValue ? userMap.GetValueOrDefault(userId.Value) : null;
                return OAuthTokenApplicationMapper.ToListItemDto(
                    token,
                    appMap.GetValueOrDefault(token.ClientId),
                    session,
                    user,
                    now);
            })
            .ToList();

        return new PageResultDtoBase<OAuthTokenListItemDto>(items, tokenPage.Page);
    }

    /// <summary>
    /// 获取 OAuth Token 详情
    /// </summary>
    /// <param name="id">OAuth Token 主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth Token 详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.OAuthToken.Read)]
    public async Task<OAuthTokenDetailDto?> GetOAuthTokenDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "OAuth Token 主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var token = await _oauthTokenRepository.GetByIdAsync(id, cancellationToken);
        if (token is null)
        {
            return null;
        }

        var app = await _oauthAppRepository.GetFirstAsync(item => item.ClientId == token.ClientId, cancellationToken);
        var session = token.SessionId.HasValue
            ? await _userSessionRepository.GetByIdAsync(token.SessionId.Value, cancellationToken)
            : null;
        var userId = token.UserId ?? session?.UserId;
        var user = userId.HasValue ? await _userRepository.GetByIdAsync(userId.Value, cancellationToken) : null;
        return OAuthTokenApplicationMapper.ToDetailDto(token, app, session, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建 OAuth Token 分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="now">当前时间</param>
    /// <returns>OAuth Token 分页请求</returns>
    private static BasicAppPRDto BuildOAuthTokenPageRequest(OAuthTokenPageQueryDto input, DateTimeOffset now)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.ClientId))
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.ClientId, input.ClientId.Trim());
        }

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.UserId, input.UserId.Value);
        }

        if (input.SessionId.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.SessionId, input.SessionId.Value);
        }

        if (input.GrantType.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.GrantType, input.GrantType.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.Status, input.Status.Value);
        }

        if (input.IsRevoked.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.IsRevoked, input.IsRevoked.Value);
        }

        if (input.IsAccessTokenExpired.HasValue)
        {
            request.Conditions.AddFilter(
                (SysOAuthToken token) => token.AccessTokenExpirationTime,
                now,
                input.IsAccessTokenExpired.Value ? QueryOperator.LessThanOrEqual : QueryOperator.GreaterThan);
        }

        if (input.IsRefreshTokenExpired.HasValue)
        {
            request.Conditions.AddFilter(
                (SysOAuthToken token) => token.RefreshTokenExpirationTime,
                now,
                input.IsRefreshTokenExpired.Value ? QueryOperator.LessThanOrEqual : QueryOperator.GreaterThan);
        }

        if (input.AccessTokenExpirationTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.AccessTokenExpirationTime, input.AccessTokenExpirationTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.AccessTokenExpirationTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.AccessTokenExpirationTime, input.AccessTokenExpirationTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        if (input.RefreshTokenExpirationTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.RefreshTokenExpirationTime, input.RefreshTokenExpirationTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.RefreshTokenExpirationTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysOAuthToken token) => token.RefreshTokenExpirationTime, input.RefreshTokenExpirationTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort((SysOAuthToken token) => token.IsRevoked, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysOAuthToken token) => token.AccessTokenExpirationTime, SortDirection.Descending, 1);
        request.Conditions.AddSort((SysOAuthToken token) => token.CreatedTime, SortDirection.Descending, 2);
        return request;
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(OAuthTokenPageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateOptionalId(input.SessionId, nameof(input.SessionId), "会话主键必须大于 0。");
        ValidateMaxLength(input.ClientId, 100, nameof(input.ClientId), "客户端 ID 长度不能超过 100。");
        if (input.GrantType.HasValue)
        {
            ValidateEnum(input.GrantType.Value, nameof(input.GrantType));
        }

        if (input.Status.HasValue)
        {
            ValidateEnum(input.Status.Value, nameof(input.Status));
        }

        ValidateRange(input.AccessTokenExpirationTimeStart, input.AccessTokenExpirationTimeEnd, nameof(input.AccessTokenExpirationTimeStart), "访问令牌过期开始时间不能晚于结束时间。");
        ValidateRange(input.RefreshTokenExpirationTimeStart, input.RefreshTokenExpirationTimeEnd, nameof(input.RefreshTokenExpirationTimeStart), "刷新令牌过期开始时间不能晚于结束时间。");
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
            throw new ArgumentOutOfRangeException(paramName, value, "枚举值无效。");
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
    /// 校验时间范围
    /// </summary>
    private static void ValidateRange(DateTimeOffset? start, DateTimeOffset? end, string paramName, string message)
    {
        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 构建 OAuth 应用映射
    /// </summary>
    private async Task<IReadOnlyDictionary<string, SysOAuthApp>> BuildAppMapAsync(IEnumerable<string> clientIds, CancellationToken cancellationToken)
    {
        var ids = clientIds
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
            .Select(clientId => clientId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<string, SysOAuthApp>(StringComparer.OrdinalIgnoreCase);
        }

        var apps = await _oauthAppRepository.GetListAsync(
            app => ids.Contains(app.ClientId),
            app => app.AppName,
            cancellationToken);
        return apps.ToDictionary(app => app.ClientId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 构建会话映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysUserSession>> BuildSessionMapAsync(IEnumerable<long?> sessionIds, CancellationToken cancellationToken)
    {
        var ids = sessionIds
            .Where(sessionId => sessionId > 0)
            .Select(sessionId => sessionId!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUserSession>();
        }

        var sessions = await _userSessionRepository.GetByIdsAsync(ids, cancellationToken);
        return sessions.ToDictionary(session => session.BasicId);
    }

    /// <summary>
    /// 构建用户映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysUser>> BuildUserMapAsync(IEnumerable<long?> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => userId > 0)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUser>();
        }

        var users = await _userRepository.GetByIdsAsync(ids, cancellationToken);
        return users.ToDictionary(user => user.BasicId);
    }
}
